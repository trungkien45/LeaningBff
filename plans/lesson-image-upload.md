# Plan: Upload ảnh trong bài học

## Mục tiêu

Cho phép giáo viên chèn ảnh vào nội dung bài học. Ảnh được lưu riêng tư trong file/blob storage; database lưu metadata và nội dung bài học giữ tham chiếu đến ảnh. Backend kiểm tra quyền mỗi lần upload hoặc xem ảnh.

## Thiết kế đề xuất

- Hỗ trợ nhiều ảnh trong một bài học.
- Không lưu bytes ảnh trong database.
- Tạo metadata ảnh liên kết với `Lesson`, gồm mã ảnh, `LessonId`, tên blob, loại nội dung và kích thước.
- Lưu file bằng blob/file storage riêng tư. Dự án đã gọi cấu hình ABP Blob Storing trong `LearningBff/Data/LearningBffDbContext.cs`; cần kiểm tra provider và cấu hình thực tế trước khi triển khai.
- Dùng `Lesson.Content` để lưu nội dung bài học và tham chiếu đến ảnh được chèn từ trình soạn thảo.

## Các bước triển khai

1. **Chốt giới hạn upload**
   - Chọn định dạng ảnh cho phép, ví dụ JPEG, PNG và WebP.
   - Chọn dung lượng tối đa cho mỗi ảnh.
   - Xác nhận có cần xóa ảnh riêng khỏi bài học hay không.

2. **Dữ liệu và lưu trữ**
   - Tạo entity/bảng metadata ảnh liên kết với bài học.
   - Tạo migration cho metadata.
   - Cấu hình blob storage riêng tư và cách tạo tên blob an toàn, duy nhất.

3. **API**
   - `POST` upload ảnh theo bài học, nhận multipart file; trả JSON metadata hoặc đường dẫn để trình soạn thảo chèn ảnh.
   - `GET` ảnh theo mã ảnh; trả bytes cùng `Content-Type` phù hợp, không trả JSON.
   - Có thể thêm `DELETE` để gỡ ảnh riêng, nếu phạm vi yêu cầu.

4. **Phân quyền**
   - Giáo viên chỉ được upload, xem hoặc xóa ảnh nếu được phân công môn chứa bài học.
   - Học viên chỉ được xem ảnh nếu đang đăng ký môn chứa bài học và đăng ký còn hiệu lực.
   - Admin được xem và quản lý theo quyền admin.
   - Kiểm tra quyền trong backend ở từng request; không dùng URL blob công khai.

5. **Tích hợp trình soạn thảo**
   - Khi giáo viên chèn ảnh lúc tạo/sửa bài, trình soạn thảo upload file trước.
   - Sau khi upload thành công, chèn tham chiếu đến API lấy ảnh vào `Lesson.Content`.
   - Khi xem bài, ảnh được tải qua endpoint có kiểm tra quyền.

6. **Dọn dẹp file**
   - Xóa blob và metadata khi ảnh bị gỡ khỏi bài.
   - Xóa ảnh liên quan khi xóa bài học.
   - Khi admin xóa môn, xóa cả blob ảnh của các bài thuộc môn; cascade database không tự xóa file ngoài database.
   - Có xử lý lỗi và khả năng dọn blob nếu thao tác database thất bại giữa chừng.

7. **Kiểm thử**
   - Giáo viên đúng môn upload/xem ảnh thành công.
   - Học viên đã đăng ký xem được ảnh.
   - Từ chối giáo viên môn khác, học viên chưa đăng ký hoặc đã hủy đăng ký.
   - Từ chối file rỗng, định dạng không hỗ trợ, file vượt giới hạn; không chỉ tin vào đuôi file client gửi.
   - Kiểm tra xóa ảnh, xóa bài và xóa môn có dọn blob tương ứng.

## Tác động database

Cần migration cho bảng metadata ảnh; không lưu nội dung ảnh dạng nhị phân trong database. Bản thân quyền xem dựa trên quan hệ bài học → chương → môn, đăng ký học viên và phân công giáo viên.

## Cần xác nhận trước khi triển khai

- Định dạng được phép và dung lượng tối đa mỗi ảnh.
- Có cần nút/API xóa ảnh riêng hay chỉ xóa khi gỡ ảnh khỏi nội dung.
- Cơ chế lưu trữ blob sẽ dùng trong các môi trường triển khai.
