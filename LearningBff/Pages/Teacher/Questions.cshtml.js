(function () {
    const form = document.getElementById('importQuestionForm');
    if (!form) return;

    const elements = {
        progressContainer: document.getElementById('importProgressContainer'),
        progressMessage: document.getElementById('importProgressMessage'),
        progressBar: document.getElementById('importProgressBar'),
        errors: document.getElementById('importErrors'),
        successCount: document.getElementById('importSuccessCount'),
        errorCount: document.getElementById('importErrorCount'),
        summary: document.getElementById('importSummary'),
        submitButton: document.getElementById('importSubmitButton'),
        cancelButton: document.getElementById('importCancelButton'),
        closeButton: document.getElementById('importCloseButton')
    };
    const progressTrack = elements.progressBar.parentElement;

    let controller = null;
    let finished = false;
    let cancelled = false;

    form.addEventListener('submit', handleSubmit);
    window.openImportModal = openImportModal;
    window.closeImportModal = closeImportModal;
    window.cancelOrCloseImport = cancelOrCloseImport;

    async function handleSubmit(event) {
        event.preventDefault();
        if (controller) return;

        resetProgress();
        elements.progressContainer.classList.remove('d-none');
        elements.progressMessage.textContent = 'Đang tải tệp lên...';
        elements.submitButton.disabled = true;
        elements.cancelButton.disabled = false;
        elements.cancelButton.textContent = 'Hủy import';
        elements.closeButton.disabled = true;
        controller = new AbortController();

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: new FormData(form),
                headers: { Accept: 'text/event-stream' },
                signal: controller.signal
            });
            if (!response.ok || !response.body) {
                throw new Error('Không thể kết nối tới máy chủ để import.');
            }

            await readSseStream(response.body);
            if (!finished && !controller.signal.aborted) {
                showConnectionError('Kết nối kết thúc trước khi nhận được kết quả import.');
            }
        } catch (error) {
            if (error.name === 'AbortError' || cancelled) {
                showCancelled();
            } else {
                showConnectionError(error.message || 'Không thể hoàn tất import.');
            }
        } finally {
            controller = null;
            elements.closeButton.disabled = false;
        }
    }

    async function readSseStream(stream) {
        const reader = stream.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        while (true) {
            const { value, done } = await reader.read();
            buffer += decoder.decode(value || new Uint8Array(), { stream: !done });
            buffer = dispatchCompleteFrames(buffer);
            if (done) break;
        }
    }

    function dispatchCompleteFrames(buffer) {
        const frames = buffer.split(/\r?\n\r?\n/);
        const remainder = frames.pop() || '';
        frames.forEach(dispatchSseFrame);
        return remainder;
    }

    function dispatchSseFrame(frame) {
        let eventName = 'message';
        const dataLines = [];

        frame.split(/\r?\n/).forEach(function (line) {
            if (line.startsWith('event:')) eventName = line.slice(6).trim();
            if (line.startsWith('data:')) dataLines.push(line.slice(5).trim());
        });

        if (dataLines.length) {
            handleServerEvent(eventName, JSON.parse(dataLines.join('\n')));
        }
    }

    function handleServerEvent(eventName, payload) {
        switch (eventName) {
            case 'started':
                elements.progressMessage.textContent = payload.message;
                break;
            case 'progress':
                showProgress(payload);
                break;
            case 'complete':
                showCompletion(payload);
                break;
            case 'error':
                showFailure(payload);
                break;
        }
    }

    function showProgress(progress) {
        const percent = progress.totalRows === 0
            ? 0
            : Math.floor(progress.processedRows * 95 / progress.totalRows);

        elements.progressBar.style.width = percent + '%';
        elements.progressBar.textContent = percent + '%';
        progressTrack.setAttribute('aria-valuenow', String(percent));
        elements.successCount.textContent = String(progress.importedQuestions);
        elements.errorCount.textContent = String(progress.errorCount);
        elements.progressMessage.textContent =
            `Đang xử lý dòng ${progress.currentRow} (${progress.processedRows}/${progress.totalRows}); ` +
            `${progress.importedQuestions} câu đã lưu, ${progress.errorCount} lỗi.`;
    }

    function showCompletion(result) {
        finished = true;
        elements.progressBar.classList.remove('progress-bar-animated');
        elements.progressBar.style.width = '100%';
        elements.progressBar.textContent = '100%';
        progressTrack.setAttribute('aria-valuenow', '100');
        elements.successCount.textContent = String(result.importedQuestions);
        elements.errorCount.textContent = String(result.errors.length);
        elements.progressMessage.textContent = 'Đã xử lý xong tệp Excel.';
        elements.summary.textContent =
            `Import hoàn tất — Thành công: ${result.importedQuestions} câu hỏi; lỗi: ${result.errors.length}.`;
        elements.summary.classList.remove('d-none');
        showErrors(result.errors || []);
        elements.cancelButton.textContent = 'Tải lại danh sách';
        elements.submitButton.disabled = false;
    }

    function showFailure(failure) {
        finished = true;
        elements.progressBar.classList.remove('progress-bar-animated');
        elements.successCount.textContent = String(failure.importedQuestions || 0);
        elements.errorCount.textContent = String(failure.errorCount || 0);
        elements.progressMessage.textContent = failure.message || 'Import thất bại.';
        elements.progressMessage.classList.add('text-danger');
        elements.summary.textContent =
            `Import đã dừng — Đã lưu thành công: ${failure.importedQuestions || 0} câu hỏi; ` +
            `lỗi đã ghi nhận: ${failure.errorCount || 0}.`;
        elements.summary.classList.remove('d-none', 'alert-success');
        elements.summary.classList.add('alert-danger');
        elements.cancelButton.textContent = 'Tải lại danh sách';
        elements.submitButton.disabled = false;
    }

    function showErrors(errors) {
        errors.forEach(function (error) {
            const item = document.createElement('li');
            item.textContent = error;
            elements.errors.appendChild(item);
        });
    }

    function showConnectionError(message) {
        finished = true;
        elements.progressMessage.textContent = message;
        elements.progressMessage.classList.add('text-danger');
        elements.cancelButton.textContent = 'Tải lại danh sách';
        elements.submitButton.disabled = false;
    }

    function showCancelled() {
        finished = true;
        cancelled = true;
        elements.progressMessage.textContent =
            'Đã gửi yêu cầu hủy. Hãy tải lại danh sách để xác nhận trạng thái.';
        elements.cancelButton.textContent = 'Tải lại danh sách';
        elements.submitButton.disabled = true;
    }

    function resetProgress() {
        elements.progressMessage.textContent = 'Đang chuẩn bị...';
        elements.progressMessage.classList.remove('text-danger');
        elements.progressBar.style.width = '0%';
        elements.progressBar.textContent = '0%';
        elements.progressBar.classList.add('progress-bar-animated');
        progressTrack.setAttribute('aria-valuenow', '0');
        elements.errors.replaceChildren();
        elements.summary.textContent = '';
        elements.summary.classList.add('d-none', 'alert-success');
        elements.summary.classList.remove('alert-danger');
        elements.successCount.textContent = '0';
        elements.errorCount.textContent = '0';
        finished = false;
        cancelled = false;
    }

    function openImportModal() {
        resetImportDialog();
        document.getElementById('importQuestionModal').classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    function resetImportDialog() {
        if (controller) return;

        form.querySelector('#questionImportFile').value = '';
        elements.progressContainer.classList.add('d-none');
        resetProgress();
        elements.submitButton.disabled = false;
        elements.cancelButton.disabled = false;
        elements.cancelButton.textContent = 'Hủy';
        elements.closeButton.disabled = false;
    }

    function cancelOrCloseImport() {
        if (controller) {
            cancelled = true;
            elements.submitButton.disabled = true;
            controller.abort();
            return;
        }
        if (finished) {
            window.location.reload();
            return;
        }
        closeImportModal();
    }

    function closeImportModal() {
        if (controller) return;

        const hasImportedQuestions = Number(elements.successCount.textContent) > 0;
        document.getElementById('importQuestionModal').classList.remove('active');
        document.body.style.overflow = '';
        resetImportDialog();
        if (hasImportedQuestions) {
            window.location.reload();
        }
    }
})();
