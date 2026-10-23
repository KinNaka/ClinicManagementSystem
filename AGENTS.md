# AGENTS.md — Hướng dẫn làm việc với Clinic Management System

Áp dụng cho toàn repository. Đây là ứng dụng quản lý phòng khám có dữ liệu nhạy cảm, tiền và tồn kho; không suy đoán nghiệp vụ từ tên file hoặc tên commit.

## Bắt buộc trước khi chỉnh sửa

1. Đọc `docs/PROJECT_OVERVIEW.md`, `docs/FEATURE_INVENTORY.md`, `docs/ARCHITECTURE.md`, `docs/DATABASE.md` và `docs/DEVELOPMENT_GUIDE.md`.
2. Kiểm tra `git status`, nhánh/HEAD và diff hiện tại. Giữ nguyên thay đổi ngoài phạm vi của người dùng; không reset, clean hoặc ghi đè.
3. Tìm và đọc source hiện tại của controller, service, entity/config EF, migration/snapshot, ViewModel, view/JS và test liên quan. Tài liệu là bản đồ; nếu source mới hơn thì đối chiếu và cập nhật tài liệu, không sửa theo tài liệu cũ một cách máy móc.
4. Tra chức năng tương tự trong FEATURE_INVENTORY và Git history trước khi tạo mới. Không tự tạo lại module/action/service/model/bảng/helper đã tồn tại.
5. Nếu thông tin không xác minh được, ghi **“Chưa xác minh”**. Không tuyên bố đã test hoặc đã triển khai khi chỉ đọc code/build.

## Phạm vi và kiến trúc

- Giữ mô hình MVC một web project và các thành phần hiện tại. Không tự chuyển sang Clean Architecture, repository/UnitOfWork, SPA/API, đổi framework/provider hoặc thêm dependency ngoài yêu cầu.
- Không tự đổi schema, sửa/xóa migration đã chia sẻ, seed, import, dữ liệu thật hoặc nghiệp vụ cũ ngoài phạm vi được người dùng giao. Nếu tính năng mới thật sự cần đổi DB/kiến trúc/nghiệp vụ, xác định ảnh hưởng cụ thể trước và thực hiện đúng phạm vi đã được cho phép; không lồng refactor lớn vào bản sửa nhỏ.
- Ưu tiên tái sử dụng service/helper/partial/binder/JS đang có; chỉ sửa phần cần cho yêu cầu. Không đổi tên controller file, namespace, status hay timezone toàn bộ để làm đẹp code.
- Lưu ý tên file không khớp tên lớp: ProductController.cs → ProductsController; PatientController.cs → PatientsController; SaleController.cs → SalesController; IIventoryService.cs chứa IInventoryService.
- Nhiều action chỉ yêu cầu đăng nhập toàn cục; quyền Admin phải được kiểm ở server khi chức năng yêu cầu, không chỉ ẩn nút. Actor audit lấy từ danh tính đã đăng nhập, không nhận tùy ý từ form.

## Hợp đồng nghiệp vụ cần bảo toàn

- Nháp có `SaleDate=null`; chỉ xác nhận thành công mới chốt ngày Việt Nam, trạng thái và xuất kho trong cùng transaction. Không tính Draft/Imported vào doanh thu Confirmed.
- Giá/tổng tiền quyết định ở server; giữ giá/dòng lịch sử khi copy/edit hợp lệ, giữ logic giảm Amount/Percent/thu khác/làm tròn.
- Combo xuất theo thành phần, cộng nhu cầu chung với hàng lẻ; service không trừ kho. Không chuyển số lượng/tiền sang float/double.
- Xóa Confirmed hoàn kho theo phiếu xuất gốc, không theo combo hiện tại. Xóa phiếu nhập đã thanh toán là nghiệp vụ khác: chỉ Admin, xóa chứng từ/chi tiết và giữ tồn. Không tự đổi hai hành vi này.
- Cân kho giữ kiểm ExpectedStock và cập nhật có điều kiện; lịch tái khám giữ RowVersion; không bỏ cơ chế chống ghi đè/xác nhận lặp để vượt lỗi.
- Import giữ mã/source/tổng cũ và owner-bound preview token; không tự merge/ghi đè dữ liệu hoặc cộng lại doanh thu lịch sử.
- Lịch sử sửa sản phẩm hiện chỉ được ghi qua ProductService.UpdateAsync; không coi đây là audit toàn hệ thống, không giả định có lịch sử trước lúc triển khai.

## Database, kiểm thử và dữ liệu nhạy cảm

- Startup app có seed Identity, không phải thao tác chỉ đọc. Không dùng khởi động app/update database/import như cách kiểm tra khi user chỉ yêu cầu tài liệu/rà soát.
- Chỉ tạo migration mới cho schema change nằm trong yêu cầu; review Up/Down/snapshot, backup và kiểm DB đích trước khi áp dụng. Không dùng EnsureDeleted/EnsureCreated trên DB ứng dụng.
- Đọc test harness trước khi chạy: nhiều console test tạo/xóa DB test hoặc đọc file import; solution build/dotnet test không tự chạy chúng.
- Chạy kiểm tra phù hợp thay đổi; ghi đúng kết quả, lỗi baseline và giới hạn. Không sửa source/giá trị kỳ vọng chỉ để che test thất bại.
- Baseline ngày 24/10/2026: web Release build qua (CS8602 ProductService.cs:59); Node 37/38 qua, reset live-filter lỗi mock thiếu dataset; PatientHistory không compile do dependency binder; Inventory và PatientImport --pricing qua. Kiểm tra lại vì baseline có thể thay đổi.
- Không ghi mật khẩu, token, connection string chứa bí mật, user-secrets, raw import, DB dump, dữ liệu nhận diện bệnh nhân vào source/test/docs/log chia sẻ. Dùng dữ liệu tổng hợp hoặc đã ẩn danh được phép.

## Sau mỗi lần bổ sung/chỉnh chức năng

- Cập nhật FEATURE_INVENTORY: action/service/entity/VM/view, quyền, test có sẵn và test thực sự đã chạy.
- Cập nhật ARCHITECTURE/DATABASE/DEVELOPMENT_GUIDE/PROJECT_OVERVIEW nếu thay đổi hợp đồng, schema, cấu hình, cách chạy hoặc giới hạn.
- Báo rõ thay đổi, lý do, kiểm chứng và vấn đề còn **Chưa xác minh**. Không coi commit message hoặc lời báo trước đó là bằng chứng Production đang chạy đúng.
- Kiểm diff để không lẫn thay đổi khác. Không tự commit/push, đổi nhánh hoặc triển khai nếu chưa được giao; không sửa lịch sử Git.
