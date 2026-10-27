# PROJECT OVERVIEW — Clinic Management System

Ngày rà soát: **24/10/2026**, múi giờ Asia/Saigon. Mốc source: nhánh `main`, commit `89a068d4fb248c66d7210ce65c29d92cfa4e7d60`. Working tree sạch khi bắt đầu rà soát. Đây là ảnh chụp trạng thái repository, không phải chứng nhận hệ thống Production.

## Đọc lại sau thời gian dài

1. Đọc tài liệu này và [FEATURE_INVENTORY](FEATURE_INVENTORY.md) để tránh viết lại chức năng.
2. Đọc [ARCHITECTURE](ARCHITECTURE.md) và [DATABASE](DATABASE.md) trước khi chạm nghiệp vụ hoặc dữ liệu.
3. Làm theo [DEVELOPMENT_GUIDE](DEVELOPMENT_GUIDE.md); đối chiếu source hiện tại, Git và database thực tế trước khi sửa.
4. Tuân thủ [AGENTS.md](../AGENTS.md). Nếu source đã thay đổi sau mốc trên, tài liệu cần cập nhật theo source.

## Mục đích và phạm vi thực tế

Ứng dụng quản lý Phòng khám Lâm Thảo, giao diện tiếng Việt: quản lý bệnh nhân, danh mục thuốc/vật tư/dịch vụ/combo, tồn kho, hóa đơn bán hàng, lịch hẹn tái khám, doanh thu và tài khoản Admin/Staff. Hỗ trợ nhập dữ liệu xuất từ KiotViet bằng file `.xlsx` và in tài liệu bằng trình duyệt.

Source chưa thể hiện một hệ thống bệnh án điện tử đầy đủ: không thấy module chẩn đoán, xét nghiệm, bảo hiểm y tế, sổ khám lâm sàng riêng, thanh toán qua cổng ngân hàng, thông báo SMS/email hoặc đồng bộ API KiotViet. Không coi các mục này là backlog đã được duyệt; yêu cầu tương lai: **Chưa xác minh**.

## Công nghệ và phiên bản

Nguồn: `ClinicManagementSystem.Web/ClinicManagementSystem.Web.csproj`, `Program.cs`, thư viện trong `wwwroot/lib`, các project trong `tests/`.

| Thành phần | Trạng thái trong repository |
|---|---|
| Web | ASP.NET Core MVC + Razor Views, C#, target `net10.0`; Nullable và ImplicitUsings bật |
| Cấu trúc | Một web project trong `ClinicManagementSystem.slnx`; các console test project không được đưa vào solution |
| Database | EF Core, provider SQL Server; Development có bộ giải quyết named pipe LocalDB |
| Identity | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` **10.0.12**, cookie auth |
| EF packages | `Microsoft.EntityFrameworkCore.SqlServer`, `.Design`, `.Tools` **10.0.11** |
| Windows Service | `Microsoft.Extensions.Hosting.WindowsServices` **10.0.12** |
| Package khác | `System.Security.Cryptography.Pkcs` **9.0.21**; mục đích sử dụng trực tiếp trong mã nghiệp vụ: Chưa xác minh |
| EF CLI manifest | `ClinicManagementSystem.Web/dotnet-tools.json`, `dotnet-ef` **10.0.12**; không phải manifest `.config/dotnet-tools.json` thông thường |
| Frontend | Bootstrap **5.3.3**, jQuery **3.7.1**, jQuery Validation **1.21.0**, Unobtrusive Validation **4.0.0**, JavaScript/CSS nội bộ |
| Import Excel | Tự đọc ZIP/XML của XLSX bằng thư viện nền tảng .NET; không thấy EPPlus/ClosedXML trong csproj |
| Test | Node `node:test` và console C# với assertion/exception thủ công; không phải xUnit/NUnit |
| Máy rà soát | SDK **10.0.401**, .NET/ASP.NET runtime **10.0.12**, Node **24.22.1**; không có `global.json` |

`dotnet --info` có lỗi quyền truy cập Service Control Manager khi lấy thông tin workload; vẫn trả phiên bản SDK/runtime. Không coi lỗi này là lỗi biên dịch ứng dụng.

## Các module thực tế

| Module | Chức năng đã có trong source |
|---|---|
| Tài khoản | Login/logout; Admin đổi mật khẩu của mình; tạo Admin/Staff; đổi vai trò; khóa/mở khóa; xóa Staff khác; bảo vệ Admin hoạt động cuối cùng |
| Tổng quan/doanh thu | Admin thấy tổng bệnh nhân/sản phẩm, hàng sắp hết, hóa đơn/doanh thu hôm nay và 7 ngày; Staff được chuyển sang doanh thu ngày/tuần |
| Bệnh nhân | Tạo/sửa/xem/tìm kiếm, trạng thái hoạt động; mã LT tự sinh; ngày sinh; CCCD/ngày cấp; địa chỉ tỉnh/xã; kiểm tra SĐT trùng, xác nhận người thân dùng chung; tạo nhanh từ hóa đơn; lịch sử hóa đơn và tổng bán |
| Danh mục | Liệt kê/thêm/sửa/xóa/đổi trạng thái nhóm; không xóa nhóm đang có sản phẩm |
| Sản phẩm | Thuốc/vật tư, dịch vụ không trừ kho, combo một cấp; giá vốn/giá bán/tồn; lọc; Admin sửa/đổi trạng thái/xóa; lịch sử sửa từng trường chỉ Admin xem |
| Kho | Xem tồn/sắp hết; nhập/xuất nhiều dòng bằng tìm kiếm; xuất gắn bệnh nhân; kiểm kho/cân tồn; lịch sử giao dịch; danh sách phiếu nhập riêng; Admin đánh dấu đã thanh toán và xóa phiếu nhập đã thanh toán |
| Hóa đơn | Nháp chưa có ngày bán; tạo/sửa/sao chép; giảm giá tiền/% và thu khác; xác nhận mới chốt ngày bán và xuất kho; tìm kiếm/lọc thời gian/trạng thái; xóa theo quyền; in hóa đơn/đơn thuốc/danh sách sản phẩm được chọn |
| Tái khám | Tạo/sửa lịch từ hóa đơn; ngày tái khám tự tính từ ngày bắt đầu + số ngày thuốc hoặc tự chọn; Pending/Attended/Cancelled; lọc, phân trang, chống ghi đè bằng rowversion |
| Import KiotViet | Preview và confirm có token gắn người dùng; khách hàng/tổng bán cũ; danh mục/sản phẩm/tồn ban đầu tùy chọn; hóa đơn lịch sử Imported không trừ kho và không cộng lại doanh thu Confirmed |
| Giao diện dùng chung | Phân trang/sắp xếp; lọc trực tiếp; tìm kiếm bỏ dấu tiếng Việt; nhập số nguyên; giao diện mobile, in và layout đăng nhập |

Chi tiết đường dẫn và trạng thái kiểm thử của từng chức năng: [FEATURE_INVENTORY](FEATURE_INVENTORY.md).

## Kết quả kiểm tra có thể tái lập

Các lệnh được chạy trong lượt bàn giao này; không chạy ứng dụng, seed, import hoặc migration và không sửa database.

| Kiểm tra | Kết quả ngày 24/10/2026 |
|---|---|
| `dotnet build ClinicManagementSystem.Web -c Release --no-restore` | Thành công; 1 warning CS8602 tại `Services/ProductService.cs:59` (`x.Category.Name`) |
| `node --test tests/*.test.cjs` | **38 ca: 37 qua, 1 lỗi** |
| `dotnet run --project tests/Inventory --no-restore` | Qua kiểm tra sáu khoảng ngày, chuyển năm, năm nhuận và cận kết thúc loại trừ |
| `dotnet run --project tests/PatientImport -c Release --no-restore -- --pricing` | Qua sắp xếp/SQL translation, CCCD, phân trang, tính giảm giá/thu khác/làm tròn; warning NU1900 do không tải được thông tin lỗ hổng từ NuGet |
| `dotnet run --project tests/PatientHistory --no-restore` | Không build được: CS0234, thiếu ASP.NET Core và kiểu binder được tham chiếu bởi `PatientFormViewModel` |
| Kiểm thử tích hợp SQL Server/LocalDB | Không chạy trong lượt này vì có tạo/xóa database test; hoạt động thực tế: **Chưa xác minh** |
| Schema/migration đã áp dụng ở DB chạy thật | **Chưa xác minh**; mới kiểm tra model, cấu hình và migrations trên đĩa, không truy vấn bảng bệnh nhân |
| Browser end-to-end, máy in, Production | **Chưa xác minh** |

## Điểm cần tiếp tục kiểm tra, không tự sửa khi quay lại

- Test reset trong `tests/live-filter.test.cjs` lỗi `Cannot read properties of undefined (reading 'defaultValue')`: mock field không có `dataset`, còn `site.js` dùng `field.dataset.defaultValue`. Đã xác nhận lỗi test; lỗi cùng hành vi trên browser thực: **Chưa xác minh**.
- `PatientHistory.csproj` link trực tiếp ViewModel mới có binder nhưng thiếu dependency tương ứng; bộ test này không chạy được nguyên trạng.
- Lịch sử sửa sản phẩm chỉ được ghi trong `ProductService.UpdateAsync`; không phải audit toàn hệ thống. Thay đổi do nhập kho, import, SQL trực tiếp không đi qua cơ chế này. Không khôi phục được lịch sử trước khi bật tính năng.
- StockIn/StockOut thủ công dùng transaction mặc định và đọc/sửa entity; chưa có cùng cơ chế cập nhật có điều kiện như xác nhận bán hàng/cân kho. An toàn khi nhiều người thao tác kho đồng thời: **Chưa xác minh**.
- Mã sản phẩm và mã nhóm được kiểm tra ở service nhưng không có unique index trong snapshot. Không được khẳng định DB bảo đảm không trùng; cần kiểm tra dữ liệu/cạnh tranh trước khi bổ sung ràng buộc.
- Status dùng cả `Active`/`ACTIVE` và `Inactive`/`INACTIVE`; một số truy vấn/toggle chỉ so một dạng. Độ nhạy hoa thường của collation DB chạy thật: **Chưa xác minh**.
- Danh sách phiếu nhập lọc mọi `StockIn`, gồm cả phiếu hoàn kho do xóa hóa đơn; không có phân loại nhà cung cấp/công nợ riêng. Đã thanh toán là dấu xác nhận, không phải bút toán kế toán.
- Xóa phiếu nhập là xóa vĩnh viễn cả chi tiết nhưng **không hoàn/trừ tồn kho**; lịch sử kho cũng mất phiếu đó. Không diễn giải thành hủy nhập hàng.
- Có view `Views/Inventory/Details.cshtml` dạng khung mẫu nhưng không có action Details tương ứng; Home/Privacy và layout mẫu còn trong source, không phải module nghiệp vụ hoàn chỉnh.
- Tài liệu triển khai cũ tham chiếu đường dẫn máy cá nhân, bản publish và `Tao-Database.sql`; các artifact triển khai và tình trạng máy chủ: **Chưa xác minh**. `startmvc.vbs` có đường dẫn ổ đĩa cố định.
- Không thấy pipeline CI được Git theo dõi. Restore/build tự động, backup/restore thực tế, SSL, quản lý khóa cookie và người phụ trách vận hành: **Chưa xác minh**.

## Git: mốc để lần lại quyết định

Đã đọc log local `--all`: 56 commit. Không fetch remote trong lượt này; độ đồng bộ với remote: **Chưa xác minh**. Commit message là manh mối, source và diff mới là bằng chứng chức năng.

| Mốc | Nội dung lần lại |
|---|---|
| `be534c8` 07/09/2026 | Initial commit |
| `ee2870e` 10/09/2026 | Migration ban đầu |
| `ca56d25`, `aae7826`, `dcb5477` 11/10/2026 | Kho, bệnh nhân, hóa đơn + kho |
| `1589c7e`, `a073b81`, `e512e9b` 11/10/2026 | Identity, phân quyền, quản lý tài khoản |
| `5f7d6c8`, `3884b09`, `01f9a13` 12/10/2026 | Import và lịch sử hóa đơn |
| `155e1ea` 13/10/2026 | Mốc triển khai; không xác nhận đang chạy commit này |
| `215621a`, `e665958`, `de8bba9` 14/10/2026 | Tạo nhanh bệnh nhân và luồng hóa đơn |
| `e60389d`, `6683700`, `2ae4ace`, `783f2b0` 16/10/2026 | Phân trang/sắp xếp, quyền xóa nháp, tái khám |
| `d22d64e` 18/10/2026 | Phiếu nhập riêng/thanh toán/xóa |
| `89a068d` 24/10/2026 | Audit sửa sản phẩm, ngày xác nhận, tồn trong tìm hóa đơn |

Không có căn cứ xác nhận lịch sử Git giải thích đầy đủ mọi quyết định nghiệp vụ. Khi bổ sung tính năng, đọc `git log -- <path>` và `git show <commit> -- <path>` thay vì suy diễn từ tên commit.

## Cập nhật chức năng 27/10/2026

Bổ sung tab Lưu trữ cho sản phẩm ngừng kinh doanh, tìm kiếm/phân trang/tồn và Admin kích hoạt lại; danh sách thao tác sản phẩm/kho và cảnh báo sắp hết mặc định chỉ hàng đang kinh doanh. Admin kiểm/cân hàng lưu trữ còn tồn qua tab riêng. Không đổi schema, lịch sử chứng từ hoặc logic sản phẩm cũ trên hóa đơn. Audit trạng thái nay còn được ghi qua ProductService.ToggleStatusAsync. Chi tiết source và bằng chứng test/LocalDB bị chặn: FEATURE_INVENTORY, mục 27/10/2026. Browser/Production: **Chưa xác minh**.
