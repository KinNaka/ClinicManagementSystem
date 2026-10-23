# FEATURE INVENTORY

Mốc kiểm kê: **24/10/2026, `main@89a068d`**. Danh sách dưới đây phản ánh action và code thực tế, không phải danh sách ý tưởng.

## Cách đọc trạng thái

- **Có code:** đã đối chiếu action/service và các kiểu/view liên quan tại commit trên.
- **Có test:** có source test liên quan; không đồng nghĩa test đã chạy hoặc phủ toàn bộ chức năng.
- **Đã chạy:** chỉ các lệnh ghi ở bảng bằng chứng cuối tài liệu này. Test mô phỏng DOM không thay cho kiểm thử browser; test tính toán không thay cho database transaction.
- **Đã xác nhận hoạt động trên app/Production:** **Chưa xác minh cho tất cả các dòng** trong lần rà soát này. Không chạy website vì startup seed Identity; không tạo/xóa database thử nghiệm theo yêu cầu chỉ làm tài liệu.

### Quy ước đường dẫn

Tất cả đường dẫn C/S/E/VM/V dưới đây nằm dưới `ClinicManagementSystem.Web/`:

- **C:** `Controllers/<tên file>.cs`. File ProductController → lớp ProductsController; PatientController → PatientsController; SaleController → SalesController.
- **S:** `Services/<tên>.cs`; interface cùng thư mục (kho là file `IIventoryService.cs`). Ghi “DbContext trực tiếp” nghĩa là không có service riêng, không được suy đoán thêm lớp.
- **E:** `Models/Entities/<tên>.cs`, ngoại trừ ApplicationUser ở `Models/Identity/`.
- **VM:** `Models/ViewModels/<nhóm>/<tên>.cs`. Một file có thể khai báo nhiều kiểu; Appointments dùng `AppointmentViewModels.cs`.
- **V:** `Views/<thư mục>/<tên>.cshtml`; `—` nghĩa không có view riêng, dùng redirect/JSON hoặc partial.
- Test C# nằm trong `tests/PatientImport/` trừ khi ghi khác. Trạng thái “có test, chưa chạy” là **Chưa xác minh** về kết quả ở commit này.

## Tài khoản và tổng quan — tất cả đã có code

| Chức năng | C / S | E / VM | V | Bằng chứng kiểm thử |
|---|---|---|---|---|
| Login/logout, lockout, kiểm active, returnUrl local | AccountController / SignInManager, UserManager | ApplicationUser / Account/LoginViewModel | Account/Login; logout redirect | AccountSecurityTests.cs có kiểm tra liên quan, chưa chạy; browser Chưa xác minh |
| Admin đổi mật khẩu của mình | AccountController.ChangePassword / UserManager | ApplicationUser / Account/ChangePasswordViewModel | Account/ChangePassword | AccountSecurityTests.cs, chưa chạy |
| Liệt kê/tạo tài khoản Admin/Staff | UserManagementController.Index/Create / UserManager | ApplicationUser, Identity roles / UserManagement/UserListViewModel, CreateUserViewModel | UserManagement/Index, Create | AccountSecurityTests.cs, chưa chạy |
| Đổi role, khóa/mở, bảo vệ admin cuối, xóa Staff khác | UserManagementController.ChangeRole/ToggleActive/Delete / UserManager | ApplicationUser / UserManagement/UserListViewModel | UserManagement/Index | AccountSecurityTests.cs, chưa chạy |
| Dashboard Admin, số liệu và doanh thu 7 ngày | DashboardController.Index / DashboardService.GetDashboardAsync | Sale, Product, Patient / Dashboard/DashboardViewModel | Dashboard/Index | DashboardRoleTests.cs có kiểm tra quyền; chưa chạy DB/browser |
| Doanh thu hôm nay/tuần cho Staff và Admin | DashboardController.Revenue / DashboardService.GetStaffRevenueAsync | Sale / Dashboard/StaffRevenueViewModel | Dashboard/Revenue | DashboardRoleTests.cs, SaleCopyTests.cs có kiểm tra liên quan; chưa chạy |

## Bệnh nhân — tất cả đã có code

| Chức năng | C / S | E / VM | V | Bằng chứng kiểm thử |
|---|---|---|---|---|
| Danh sách, tìm mã/tên/SĐT, trạng thái, paging/sort | PatientController.Index / PatientService.GetAllAsync | Patient, Sale / Patients/PatientIndexViewModel, PatientListViewModel | Patients/Index | PagingTests.cs/SortingTests.cs đã qua phần helper; DB/browser Chưa xác minh |
| Tạo/sửa hồ sơ và mã LT, trạng thái | PatientController.Create/Edit/SetStatus / PatientService.CreateAsync/UpdateAsync/GeneratePatientCodeAsync/SetStatusAsync, PatientCodeGenerator | Patient / Patients/PatientFormViewModel | Patients/Create, Edit, _PatientForm | Program.cs có parser/mã LT; chưa chạy toàn bộ default harness |
| Chi tiết bệnh nhân và lịch sử mua (có nháp), tổng cũ + Confirmed | PatientController.Details / PatientService.GetDetailsAsync | Patient, Sale / Patients/PatientDetailsViewModel và PatientPurchaseHistoryViewModel; Sales/SaleListViewModel | Patients/Details | tests/PatientHistory hiện không build; integration Chưa xác minh |
| Ngày sinh dd/MM/yyyy hoặc ddMMyyyy | PatientController.ValidatePatient / PatientService; DayMonthYearDateBinder | Patient / Patients/PatientFormViewModel | Patients/_PatientForm | BirthDateTests.cs chưa chạy; birth-date.test.cjs đã qua |
| CCCD 12 chữ số và ngày cấp | PatientController.Create/Edit / PatientService | Patient / Patients/PatientFormViewModel | Patients/_PatientForm | CitizenIdentityTests.cs đã qua validation, không chứng minh lưu DB |
| Trùng SĐT, xác nhận người thân dùng chung | PatientController.PhoneMatches/Create/Edit / PatientService.FindByPhoneAsync/ValidateSharedPhoneAsync/NormalizePhone | Patient self-reference / Patients/PhoneMatchViewModel, PatientFormViewModel | Patients/_SharedPhonePopup, _PatientForm | shared-phone.test.cjs đã qua; SharedPhoneTests.cs chưa chạy DB |
| Chọn tỉnh/xã từ catalog đóng gói | PatientController.AddressProvinces/AddressWards/ValidatePatient / VietnamAddressService | Patient / Patients/PatientFormViewModel; kiểu catalog trong service | Patients/_PatientForm | vietnam-address.test.cjs đã qua catalog/DOM; lưu DB Chưa xác minh |
| Tạo nhanh bệnh nhân từ hóa đơn | PatientController.QuickCreate / PatientService | Patient / Patients/PatientFormViewModel, QuickPatientCreatedViewModel | Patients/Create, QuickCreated; Shared/_QuickPatientLayout | quick-patient.test.cjs đã qua; QuickPatientTests.cs chưa chạy |

## Danh mục và sản phẩm — tất cả đã có code

| Chức năng | C / S | E / VM | V | Bằng chứng kiểm thử |
|---|---|---|---|---|
| Nhóm sản phẩm: danh sách/thêm/sửa/xóa/toggle | ProductCategoriesController / ProductCategoryService | ProductCategory, Product / ProductCategories/ProductCategoryFormViewModel, ProductCategoryListViewModel | ProductCategories/Index, Create, Edit | Không tìm thấy bộ test CRUD nhóm riêng; Chưa xác minh runtime |
| Sản phẩm: tìm kiếm/lọc nhóm/trạng thái, paging/sort | ProductController.Index / ProductService.GetAllAsync | Product, ProductCategory / Products/ProductListViewModel | Products/Index | Helper sort/paging đã qua; test danh sách DB Chưa xác minh |
| Admin tạo/sửa/xóa/toggle sản phẩm, giá/tồn | ProductController.Create/Edit/Delete/ToggleStatus / ProductService | Product / Products/ProductFormViewModel | Products/Create, Edit | ProductImportTests.cs/ComboTests.cs có dùng service; chưa chạy integration |
| Combo một cấp và tồn suy ra từ thành phần | ProductController.Create/Edit / ProductService.ValidateComboAsync/BuildComponents; SaleService.ValidateStockAsync | Product, ComboComponent / Products/ProductFormViewModel, ComboComponentFormViewModel (cùng file) | Products/_ComboForm, Create, Edit | ComboTests.cs có kiểm tra, chưa chạy |
| Dịch vụ không quản lý/trừ kho | ProductController; SaleController.Confirm / ProductService; SaleService | Product.IsService, SaleDetail / Products/ProductFormViewModel; Sales/SaleDetailFormViewModel | Products/Create/Edit; Sales/Create/Edit | ProductImportTests.cs/ComboTests.cs có liên quan, chưa chạy |
| Admin xem lịch sử sửa từng trường/giá, lọc sản phẩm/người/thời gian | ProductController.History + Edit/ToggleStatus / ProductService.UpdateAsync ghi audit; History đọc DbContext trực tiếp | ProductEditHistory, Product / **không có VM riêng**, view nhận List<ProductEditHistory> | Products/History; nút ở Index/Edit | ProductHistoryAndSaleDateTests.cs có test role/audit/no-op, chưa chạy trong lượt này |

Audit chỉ áp dụng UpdateAsync, không ghi tạo/xóa sản phẩm và không bao phủ mọi nguồn thay đổi tồn/giá. Combo import chưa có thành phần được đặt INACTIVE, cần cấu hình lại trước khi bán.

## Kho — tất cả đã có code

| Chức năng | C / S | E / VM | V | Bằng chứng kiểm thử |
|---|---|---|---|---|
| Tồn kho và sắp hết; không gồm combo/service | InventoryController.Index / InventoryService.GetStockAsync | Product / Inventory/InventoryIndexViewModel | Inventory/Index | Helper paging/sort đã qua; query/runtime Chưa xác minh |
| Tìm và thêm/xóa nhiều dòng nhập kho | InventoryController.StockIn / InventoryService.StockInAsync | Product, InventoryTransaction, InventoryTransactionDetail / Inventory/StockInViewModel, StockInDetailViewModel | Inventory/StockIn, _StockRows; stock-form.js | search-picker.test.cjs đã qua picker; chưa có bằng chứng test transaction nhập/nhiều dòng end-to-end |
| Xuất thủ công nhiều dòng, gộp nhu cầu để kiểm tồn | InventoryController.StockOut / InventoryService.StockOutAsync | Product, InventoryTransaction, InventoryTransactionDetail / Inventory/StockOutViewModel, StockInDetailViewModel | Inventory/StockOut, _StockRows; stock-form.js | Picker đã qua; xuất đồng thời và DB Chưa xác minh |
| Gắn bệnh nhân cho phiếu xuất, tra ai/thời gian | InventoryController.StockOut/Transactions / InventoryService.StockOutAsync; Transactions đọc DbContext trực tiếp | Patient, InventoryTransaction, Sale / Inventory/StockOutViewModel; Transactions nhận List<InventoryTransaction> | Inventory/StockOut, Transactions | Chưa có test riêng được xác nhận đã chạy |
| Danh sách phiếu nhập riêng: tìm/lọc thời gian/thanh toán/tổng/chi tiết | InventoryController.Receipts / DbContext trực tiếp | InventoryTransaction/Detail, Product / **không có VM riêng** | Inventory/Receipts; lối vào Inventory/Index và Sales/Index | Không tìm thấy test riêng cho Receipts; DatePeriod helper đã qua |
| Admin đánh dấu đã thanh toán | InventoryController.MarkPaid / ExecuteUpdateAsync trực tiếp | InventoryTransaction.PaidAt/PaidBy / không VM riêng | Inventory/Receipts | Chưa xác minh transaction/role bằng request thực |
| Admin xóa phiếu nhập đã thanh toán; giữ tồn | InventoryController.DeleteReceipt / transaction + ExecuteDeleteAsync trực tiếp | InventoryTransaction/Detail / không VM riêng | Inventory/Receipts có confirm xóa | Không tìm thấy test riêng; không được coi build là bằng chứng xóa đúng trên DB |
| Lịch sử kho, loại phiếu, sản phẩm/bệnh nhân/chứng từ/thời gian | InventoryController.Transactions / DbContext trực tiếp; InventoryService.GetTransactionsAsync vẫn còn nhưng action không dùng | InventoryTransaction/Detail, Product, Patient, Sale / không VM riêng; ViewBag.LinkedSales | Inventory/Transactions | DatePeriod đã qua; lịch sử và lookup DB Chưa xác minh |
| Admin kiểm kho và cân tồn, chống ghi đè, ghi Adjustment | InventoryController.StockCount / InventoryService.GetStockCountAsync/AdjustStockAsync | Product, InventoryTransaction/Detail / Inventory/StockAdjustmentViewModel, StockCountRow (cùng file) | Inventory/StockCount | StockCountTests.cs có test, chưa chạy DB |

Phải phân biệt: xóa phiếu nhập **không** đảo hàng nhập; xóa hóa đơn Confirmed **có** hoàn theo phiếu xuất gốc. Mọi StockIn, kể cả hoàn kho do xóa hóa đơn, xuất hiện trong Receipts theo query hiện tại.

## Hóa đơn — tất cả đã có code

| Chức năng | C / S | E / VM | V | Bằng chứng kiểm thử |
|---|---|---|---|---|
| Danh sách/tìm hóa đơn, sản phẩm, bệnh nhân; trạng thái và sáu khoảng ngày | SaleController.Index / SaleService.GetAllAsync | Sale, SaleDetail, Patient, Product / Sales/SaleIndexViewModel, SaleListViewModel | Sales/Index | DatePeriod, paging/sort helper đã qua; SQL query và app Chưa xác minh |
| Tạo nháp nhiều dòng, tìm sản phẩm kèm giá/đơn vị/tồn | SaleController.Create/GetProductPrice / SaleService.PrepareFormAsync/CreateAsync/GetProductPriceAsync | Sale/Detail, Product, Patient / Sales/SaleFormViewModel, SaleDetailFormViewModel | Sales/Create; search-picker.js | sale-product-search.test.cjs, search-picker.test.cjs đã qua; label tồn DB có test ở ProductHistoryAndSaleDateTests.cs, chưa chạy |
| Sửa nháp, giữ giá cũ của dòng hợp lệ | SaleController.Edit / SaleService.GetDraftAsync/UpdateDraftAsync/BuildDetailsAsync | Sale/Detail / Sales/SaleFormViewModel, SaleDetailFormViewModel | Sales/Edit | SaleCopyTests.cs, SaleAdjustmentTests.cs có test, chưa chạy |
| Sao chép hóa đơn nguồn thành nháp mới | SaleController.Create(copyFromId) / SaleService.PrepareCopyAsync/CreateAsync | Sale/Detail / Sales/SaleFormViewModel, SaleDetailFormViewModel | Sales/Details → Create | SaleCopyTests.cs có test, chưa chạy |
| Giảm Amount/Percent, thu khác, làm tròn/tổng server | SaleController.Create/Edit / SaleService, SalePricing | Sale/Detail / Sales/SaleFormViewModel | Sales/Create, Edit; sale-pricing.js | SalePricingTests.cs và sale-pricing.test.cjs đã qua; persist DB Chưa xác minh |
| Nháp không có ngày bán; xác nhận mới gán ngày Việt Nam | SaleController.Confirm / SaleService.ConfirmAsync | Sale.SaleDate nullable / Sales/SaleDetailsViewModel, SaleListViewModel | Sales/Index, Details, các Print | ProductHistoryAndSaleDateTests.cs có test xác nhận/lỗi/lặp, chưa chạy; migration backfill tồn tại |
| Xác nhận và xuất thành phần combo/hàng lẻ; không xuất dịch vụ | SaleController.Confirm / SaleService.ConfirmAsync/ValidateStockAsync | Sale/Detail, Product, ComboComponent, InventoryTransaction/Detail / Sales/SaleDetailsViewModel | Sales/Details | ComboTests.cs có test, chưa chạy |
| Staff xóa nháp, Admin xóa nháp/Confirmed/Imported; hoàn kho đúng nguồn | SaleController.Delete / SaleService.DeleteAsync | Sale/Detail, InventoryTransaction/Detail, FollowUpAppointment, Product / Sales/SaleIndexViewModel | Sales/Index, Details | StaffDraftDeletionTests.cs, SaleDeletionTests.cs có test; chưa chạy trong lượt này |
| Xem chi tiết | SaleController.Details / SaleService.GetByIdAsync | Sale/Detail, Patient, Product / Sales/SaleDetailsViewModel + SaleDetailDisplayViewModel (cùng file) | Sales/Details | Chưa xác minh request/browser |
| In hóa đơn, đơn thuốc, chọn sản phẩm để in | SaleController.Print/PrintPrescription/PrintSelectedProducts / SaleService.GetByIdAsync | Sale/Detail, Patient, Product / Sales/SaleDetailsViewModel | Sales/Print, PrintPrescription, PrintSelectedProducts | Không thấy test máy in/layout riêng; browser/máy in Chưa xác minh |

## Lịch hẹn tái khám — tất cả đã có code

| Chức năng | C / S | E / VM | V | Bằng chứng kiểm thử |
|---|---|---|---|---|
| Danh sách: mặc định từ hôm nay, ngày từ/đến, người/điện thoại/mã hóa đơn, trạng thái/sort/page | AppointmentsController.Index / AppointmentService.GetListAsync | FollowUpAppointment, Patient / Appointments/AppointmentViewModels.cs: AppointmentListViewModel, AppointmentRowViewModel | Appointments/Index | AppointmentTests.cs, AppointmentViewTests.cs có test, chưa chạy |
| Tạo/sửa lịch theo hóa đơn; số ngày thuốc hoặc tự chọn ngày; optimistic concurrency | AppointmentsController.Edit / AppointmentService.GetFormAsync/SaveAsync | FollowUpAppointment, Sale / AppointmentFormViewModel trong AppointmentViewModels.cs | Appointments/Edit; appointment-form.js | AppointmentTests.cs, AppointmentViewTests.cs, chưa chạy |
| Ghi nhận đến/hủy, giữ audit người/thời gian | AppointmentsController.ChangeStatus / AppointmentService.ChangeStatusAsync | FollowUpAppointment / AppointmentRowViewModel | Appointments/Index, Edit | AppointmentTests.cs, chưa chạy |

Chưa có module đặt lịch độc lập không cần hóa đơn, nhắc hẹn tự động hoặc gửi SMS trong source. Đây là giới hạn phạm vi hiện tại, không phải chức năng dở dang đã được phê duyệt.

## Import KiotViet — tất cả đã có code

| Chức năng | C / S | E / VM | V | Bằng chứng kiểm thử |
|---|---|---|---|---|
| Khách hàng XLSX: preview/confirm, giữ mã/tổng bán cũ, bỏ mã tồn tại | PatientImportsController.Index/Preview/Confirm / PatientImportService, KiotVietCustomerReader, VietnamAddressService | Patient / Patients/PatientImportViewModel + các kiểu row trong file | PatientImports/Index | Program.cs có parser tests; không chạy parser/default/integration trong lượt này |
| Admin nhập sản phẩm/nhóm, tùy chọn tồn và xử lý tồn âm | ProductImportsController.Index/Preview/Confirm / ProductImportService, KiotVietProductReader | Product, ProductCategory / Products/ProductImportViewModel + row | ProductImports/Index | ProductImportTests.cs cần dữ liệu/DB phù hợp; chưa chạy |
| Hóa đơn lịch sử nhiều file, liên kết mã, tùy chọn khách lẻ/tạo thiếu | SaleImportsController.Index/Preview/Confirm / SaleImportService, KiotVietSaleReader, KiotVietSheetReader | Sale/Detail, Patient, Product, ProductCategory / Sales/SaleImportViewModel + các kiểu invoice trong file | SaleImports/Index | SaleImportTests.cs và Program.cs nhánh --sales có test; chưa chạy |

Giới hạn controller: customer/product `.xlsx` ≤10 MB/file (request 12 MB); invoice 1–4 file, ≤20 MB/file và ≤60 MB/tổng (request 64 MB). Reader có giới hạn dòng/giải nén riêng. Token owner-bound giữ trong RAM. Imported **không** trừ kho hoặc cộng lại LegacyTotalSales. Không có API sync KiotViet.

## Thành phần UI/hạ tầng hiện có

| Chức năng | File/type cần tái sử dụng | Trạng thái kiểm tra |
|---|---|---|
| Live GET filter/debounce/stale response | wwwroot/js/site.js; form data-live-filter/data-results-id | 5/6 ca trong live-filter.test.cjs qua; reset lỗi do fixture thiếu dataset, browser Chưa xác minh |
| Picker bỏ dấu, touch, keyboard, ID selection, refresh | wwwroot/js/search-picker.js; select data-search-picker | search-picker.test.cjs qua |
| Stepper và số nguyên | wwwroot/js/quantity-stepper.js; QuantityModelBinder; WholeQuantityAttribute | quantity-stepper.test.cjs qua; server WholeQuantityTests.cs chưa chạy |
| Sort/page/query retention | Services/ListSorting.cs, ListPaging.cs; Views/Shared/_ListSort, _ListSortForm, _ListPaging | SortingTests.cs/PagingTests.cs qua helper; không đại diện mọi list |
| Bộ lọc thời gian | Services/DatePeriod.cs; Views/Shared/_DatePeriod | tests/Inventory qua |
| Responsive cards/layout | wwwroot/js/mobile-layout.js; css/dashboard.css, clinic.css, mobile.css; Shared/_Layout | mobile-layout.test.cjs qua; thiết bị/browser thật Chưa xác minh |
| LocalDB pipe resolver | Data/DevelopmentLocalDbConnection.cs, Program.cs | Có nhánh --localdb trong test harness, chưa chạy trong lượt này |

## Bằng chứng chạy test và việc còn thiếu

Ngày 24/10/2026: web Release build thành công (CS8602 ở ProductService.cs:59); Node 37/38 qua, lỗi reset ở `tests/live-filter.test.cjs:70`; test Inventory qua; lệnh `PatientImport --pricing` chạy sắp xếp, CCCD, phân trang và pricing thành công. Test PatientHistory không compile được (thiếu dependency ASP.NET Core/binder).

Chưa có bằng chứng kiểm thử thành công trong lượt này cho: transaction nhập/xuất thủ công, xóa/thanh toán phiếu nhập, migrations nâng DB thực, tính đúng doanh thu sau xác nhận/xóa trên DB, audit giữa hai tài khoản trên app, calendar concurrency, import file thực, bảo mật qua HTTP request, print và trình duyệt mobile.

Các điểm cần xử lý khi được giao yêu cầu tiếp theo:

1. Sửa/đồng bộ test harness reset và PatientHistory rồi chạy lại; không thay nghiệp vụ chỉ để test xanh.
2. Xác minh DB đã có migration cuối trước khi đánh giá lỗi ngày nháp/audit.
3. Bổ sung bằng chứng integration có DB riêng, đặc biệt DeleteReceipt/MarkPaid và nhập/xuất đồng thời.
4. Audit không phải nhật ký toàn hệ thống, không có dữ liệu quá khứ trước lúc bật tính năng.
5. `Inventory/Details.cshtml` là placeholder không có action; Home/Privacy là template, không ghi là tính năng đã hoàn thiện.
6. Roadmap nghiệp vụ, mức ưu tiên và người nghiệm thu: **Chưa xác minh**. Các giới hạn trên không tự cho phép AI triển khai thêm tính năng ngoài yêu cầu.
