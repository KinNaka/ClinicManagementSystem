# DEVELOPMENT GUIDE

Ngày kiểm tra: **24/10/2026**, source `main@89a068d`. Đây là hướng dẫn quay lại làm việc với repository hiện tại; các lệnh sửa DB/triển khai bên dưới là quy trình cho lần được giao việc tương ứng, **không được thực hiện trong lượt tạo tài liệu này**.

## 1. Bắt đầu lại sau 6–12 tháng

1. Đọc [PROJECT_OVERVIEW](PROJECT_OVERVIEW.md), [FEATURE_INVENTORY](FEATURE_INVENTORY.md), [ARCHITECTURE](ARCHITECTURE.md), [DATABASE](DATABASE.md), rồi [AGENTS.md](../AGENTS.md).
2. Kiểm tra Git và các thay đổi chưa commit, không reset/clean hoặc ghi đè công việc của người khác.
3. Định vị chức năng mới trong feature inventory, tìm lại action/service/type/view/test thực tế. File controller có tên số ít nhưng lớp/route số nhiều; interface kho ở `IIventoryService.cs`.
4. Xác định commit và migration đang chạy trên máy đích. Trạng thái Production, DB backup, user nghiệm thu và cấu hình máy chủ hiện tại đều **Chưa xác minh** trong lần bàn giao.
5. Chạy build và các test không DB trước; giữ log thất bại để so sánh với baseline. Không khởi động app/import/seed chỉ để “xem có chạy không” khi yêu cầu đang là chỉ đọc.

```powershell
git status --short
git branch --show-current
git log -12 --oneline
git log -- ClinicManagementSystem.Web/Services/SaleService.cs
git show 89a068d --stat
```

Nguồn đang kiểm kê có 56 commit local, mốc mới nhất 89a068d. Không có xác nhận đã fetch/push hoặc remote bằng source local. Không coi tên commit là test report.

## 2. Công cụ và mở solution

- Cài .NET SDK hỗ trợ **net10.0** và IDE hỗ trợ `.slnx`/.NET 10; phiên bản Visual Studio tối thiểu trên máy thật: **Chưa xác minh**. Có thể mở thư mục bằng editor và dùng CLI, không phụ thuộc IDE.
- Máy rà soát dùng SDK 10.0.401, runtime 10.0.12. Không có global.json; máy mới có thể tự chọn SDK khác.
- SQL Server/Express hoặc LocalDB cho Development; SSMS/sqlcmd khi quản trị bằng quyền phù hợp. Không dùng LocalDB cho IIS nhiều máy như thể là SQL Server instance dùng chung.
- Node cho test JavaScript; bản đã dùng là 24.22.1. Không thấy package.json/npm build trong repository; vendor frontend đã đóng gói.
- Mở `ClinicManagementSystem.slnx` từ thư mục gốc. Solution chỉ chứa web; test project phải chạy riêng.

```powershell
dotnet restore ClinicManagementSystem.slnx
dotnet build ClinicManagementSystem.slnx -c Release --no-restore
```

Restore cần NuGet; warning NU1900 trong lượt kiểm kê là do không lấy được dữ liệu vulnerability qua mạng, không phải bằng chứng tất cả dependency an toàn. Kiểm tra phiên bản package ở csproj trước khi nâng; không tự đồng bộ số phiên bản trong một task nghiệp vụ.

Nếu build báo file `.exe` bị app đang chạy giữ, dừng đúng instance Development hoặc build cấu hình/output riêng. Không kill toàn bộ dotnet/SQL Server và không dừng dịch vụ Production chỉ để build. Build Release của web đã qua tại mốc tài liệu này, còn CS8602 ở `ProductService.cs:59`.

### EF tooling

`ClinicManagementSystem.Web/dotnet-tools.json` khai báo dotnet-ef 10.0.12, nhưng vị trí manifest không phải `.config/dotnet-tools.json` chuẩn. Kiểm tra công cụ **thực sự được gọi**:

```powershell
dotnet ef --version
dotnet tool list --global
dotnet tool restore --tool-manifest .\ClinicManagementSystem.Web\dotnet-tools.json
```

Restore manifest ở vị trí tùy chỉnh không tự chứng minh lệnh `dotnet ef` sau đó sẽ resolve đúng tool đó. Trong các thao tác trước lần kiểm kê, EF CLI 10.0.11 từng cảnh báo runtime 10.0.12; chọn/cài công cụ rõ phiên bản trong môi trường phát triển đã được phép, không tự nâng toàn bộ package để loại warning. Chưa xác minh global tool hiện tại của mọi máy.

## 3. Cấu hình môi trường, không ghi bí mật

.NET nạp appsettings.json, appsettings theo môi trường, user-secrets trong Development và biến môi trường/CLI theo cơ chế host mặc định. Giá trị thực tế có thể bị override ngoài file Git; không suy ra DB đang dùng chỉ bằng đọc appsettings.json.

| Cấu hình | Code sử dụng | Lưu ý |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Program.cs / UseSqlServer | Bắt buộc; dùng cấu hình riêng máy, không chép secret vào tài liệu/log/Git |
| `Hosting:AllowHttp` | Cookie SecurePolicy, HTTPS redirect, HSTS | Chỉ bật khi có chủ đích chạy HTTP nội bộ; không đồng nghĩa mạng đã an toàn |
| `Urls` | Host/Kestrel | LocalNetwork có cấu hình này; binding Production thực: Chưa xác minh |
| `SeedAdmin:Email`, `SeedAdmin:Password` | IdentitySeeder | Chỉ bootstrap khi hai giá trị được điền; không có mật khẩu mặc định hợp lệ trong source cấu hình đã kiểm tra |
| `Logging`, `AllowedHosts` | Host | Đối chiếu triển khai cụ thể; không mặc định log có thể chia sẻ công khai |

Các file đã kiểm tra bằng cấu trúc khóa, không sao chép giá trị kết nối:

- appsettings.json: ConnectionStrings, Logging, AllowedHosts.
- appsettings.Development.json: Logging; không có connection override trong file này.
- appsettings.LocalNetwork.json: Hosting, Urls, SeedAdmin; email/password seed đang để trống trong file được Git theo dõi; không có connection override tại file này.
- User-secrets, biến môi trường, appsettings trên máy chủ: **Chưa xác minh**. Không đọc/in toàn bộ chúng để viết tài liệu.

Có thể quản lý bí mật Development qua user-secrets; dùng giá trị riêng máy tại thời điểm thiết lập, không paste bí mật vào trao đổi/log. Biến môi trường dùng dạng `ConnectionStrings__DefaultConnection`, `SeedAdmin__Email`, `SeedAdmin__Password`. Không để password thật trong ví dụ hoặc commit.

### Development

`launchSettings.json`:

| Profile | Environment | Bindings / tùy chọn |
|---|---|---|
| http | Development | HTTP trên mọi interface cổng 5000; Hosting__AllowHttp=true |
| https | Development | localhost HTTPS 7211 + HTTP 5229 |

Lệnh chạy khi DB/schema và seed đã được chuẩn bị:

```powershell
dotnet run --project ClinicManagementSystem.Web --launch-profile http
# Hoặc dùng profile HTTPS:
dotnet run --project ClinicManagementSystem.Web --launch-profile https
```

Lưu ý profile http bind mọi interface, không chỉ localhost. Không mở môi trường phát triển với dữ liệu thật ra mạng ngoài phạm vi được phép.

`DevelopmentLocalDbConnection.ResolveAsync` chỉ can thiệp connection Windows-authenticated trỏ `(localdb)` và chỉ trong Development: đọc pipe từ log, thử kết nối, dùng SqlLocalDB info/start nếu cần; giữ nguyên catalog/auth. Đó là helper có sẵn, không thay bằng pipe hard-code. Profile/app config của người dùng cuối có thể dùng SQL Server khác; không tự đổi sang LocalDB.

Startup luôn truy cập Identity để tạo role Admin/Staff và có thể tạo/gán Admin khi seed được điền. **Startup không tự update migrations**. Một ứng dụng chưa có schema đúng có thể lỗi trước khi hiện trang login.

### Production / LocalNetwork

- Production mặc định bật exception handler/HSTS/HTTPS redirect nếu AllowHttp không bật; cần chứng chỉ, binding và quyền DB đúng cho tài khoản chạy app.
- Code có `UseWindowsService` với service name LamThaoClinic, content root là thư mục executable khi chạy service. Việc service đã được cài/cấu hình và đang chạy: **Chưa xác minh**.
- `Chay-PhongKham.cmd` đặt Environment=LocalNetwork, chạy EXE, ghi Logs/mvc.log và restart.log, lặp khởi động lại sau 5 giây. Khi bảo trì, phải dừng vòng lặp/hệ thống tự chạy đúng cách; dừng riêng EXE có thể bị mở lại.
- `startmvc.vbs` chạy ẩn một đường dẫn ổ đĩa cố định; phải đối chiếu máy đích trước khi dùng. Không coi đường dẫn này là cấu hình portable.
- Giờ server cần được xác minh: source có cả UTC, DateTime.Now và giờ Việt Nam. Không tự sửa dữ liệu lịch sử khi thay timezone.

## 4. Database và migrations

Đọc [DATABASE](DATABASE.md) trước. Đối chiếu DB đích bằng metadata, backup/restore thử nghiệm, tạo migration mới, review SQL/data backfill trước khi apply. Không xóa migrations, snapshot hoặc database để “chạy lại từ đầu” nếu có dữ liệu cần giữ.

Ví dụ phát triển một schema change được giao:

```powershell
dotnet ef migrations add TenThayDoiCuThe --project ClinicManagementSystem.Web
dotnet ef migrations script --idempotent --project ClinicManagementSystem.Web --output migration-review.sql
# Chỉ sau khi đã xác định DB đích, backup và phạm vi triển khai:
dotnet ef database update --project ClinicManagementSystem.Web
```

Không chạy mặc định trên DB thật khi chỉ cần test. Hai script UPDATE trong docs không thay thế toàn bộ migration chain. Migration cuối sửa cả ngày của dữ liệu Draft/Confirmed; cần kiểm tra tác động báo cáo trước khi áp dụng.

## 5. Thêm tính năng không làm hỏng luồng cũ

1. Viết rõ trigger, đầu vào/đầu ra, role và tiêu chí nghiệm thu; đối chiếu FEATURE_INVENTORY. Nếu đã có chức năng tương tự, mở rộng chức năng hiện tại.
2. Đọc controller → service → entity/config → ViewModel → view/JS → test → Git history của đường xử lý. Không sửa chỉ một phía client hoặc bỏ validation server.
3. Giữ kiến trúc hiện tại; tái dùng paging/sort/date picker, validation/binder, SalePricing, chuẩn hóa SĐT, import token/reader, transaction và helper đã có.
4. Chỉ thay schema nếu cần cho yêu cầu; ghi ảnh hưởng/backfill/rollback. Giữ ngày hóa đơn, tồn kho/hoàn kho, combo/service, tiền, legacy/imported và quyền ở ARCHITECTURE như hợp đồng hiện tại.
5. Không lấy giá bán/tổng tiền/actor audit từ client làm nguồn sự thật. Thay đổi trạng thái hoặc kiểm tồn rồi ghi phải xem xét transaction và cạnh tranh.
6. Test tình huống thành công, không đủ quyền, dữ liệu sai, confirm lặp, thiếu tồn, source copy/import không còn tồn tại, xung đột lịch, rollback. Dùng dữ liệu tổng hợp, không copy hồ sơ thật vào fixture.
7. Đối chiếu hồi quy liên module: sửa sản phẩm ảnh hưởng tìm kiếm/combo/audit; sửa SaleDate ảnh hưởng doanh thu/lịch sử/in/import; xóa hóa đơn ảnh hưởng tồn và tái khám; import không được tự cộng lại tổng cũ.
8. Sau khi được phép chạy test có DB, dùng DB riêng có tên sinh cho test; kiểm tra cleanup chỉ xóa DB do chính test tạo. Không thay connection test thành DB ứng dụng.
9. Cập nhật docs cùng thay đổi: file/action, schema/migration, quy tắc, trạng thái test, hạn chế/triển khai. Không ghi “đã hoạt động” chỉ vì compile.

## 6. Kiểm thử hiện tại và cách chạy

Không có test SDK xUnit/NUnit hay pipeline CI được theo dõi. `dotnet test` ở solution không chứng minh các console/Node test đã chạy.

### Lệnh không ghi database

Chạy từ thư mục gốc:

```powershell
node --test tests/*.test.cjs
dotnet run --project tests/Inventory --no-restore
dotnet run --project tests/PatientImport -c Release --no-restore -- --pricing
```

`--pricing` đi qua cả SortingTests, CitizenIdentityTests và PagingTests trước khi chạy SalePricingTests rồi return. Nếu không có assets/dependencies, restore từng project trước; restore cần mạng/cached packages.

Có các nhánh dừng sớm `--sorting`, `--citizen-identity`, `--paging`; đọc Program.cs trước khi tổ hợp flags. Không coi mọi tham số `--...` là an toàn: harness có nhánh coi tham số lạ là đường dẫn file.

Baseline xác nhận:

| Lệnh/phạm vi | Kết quả |
|---|---|
| Web Release build | Qua, 1 CS8602 ProductService.cs:59 |
| Node toàn bộ | 37/38 qua; live-filter reset lỗi mock thiếu dataset |
| Inventory console | Qua DatePeriod |
| PatientImport --pricing | Qua sort (gồm SQL translation, không cần DB live), CCCD, paging, pricing; NU1900 mạng |
| PatientHistory console | **Không compile** vì VM link thiếu ASP.NET Core và binder dependency |

Chưa sửa các lỗi này trong lượt tài liệu. Không bỏ test lỗi hoặc thay giá trị kỳ vọng mà không hiểu hợp đồng thực tế.

### Test có database / side effect — chưa chạy trong lượt bàn giao

Đọc kỹ từng file trước khi chạy, vì một số test `EnsureCreated`/`EnsureDeleted` trên DB sinh mới, một số cần LocalDB, file export mẫu hoặc tài khoản test; `--localdb` mở DB ứng dụng để kiểm kết nối. Không chạy trên máy Production bằng thói quen.

| Nhánh harness | Source chính |
|---|---|
| --product-history-sale-date | ProductHistoryAndSaleDateTests.cs: audit 2 admin, no-op, ngày nháp/confirm/lặp/lỗi, tồn label |
| --combo | ComboTests.cs |
| --appointments | AppointmentTests.cs + AppointmentViewTests.cs |
| --sale-adjustments | SaleAdjustmentTests.cs |
| --copy | SaleCopyTests.cs |
| --delete-sales | SaleDeletionTests.cs |
| --stock-count | StockCountTests.cs |
| --accounts | AccountSecurityTests.cs |
| --shared-phone / --quick-patient | SharedPhoneTests.cs / QuickPatientTests.cs |
| --roles / --staff-draft-deletion | DashboardRoleTests.cs / StaffDraftDeletionTests.cs; đọc implementation để biết có cần DB hay chỉ reflection/proxy |
| --products / --sales | ProductImportTests.cs / SaleImportTests.cs, có tham số dữ liệu; không đoán đường dẫn/fixtures |
| --integration / --localdb | Program.cs; chỉ chạy trong môi trường phù hợp đã xác định |

Không lưu raw output có tên/SĐT/địa chỉ/CCCD từ file import thật vào test report hoặc docs. Browser/print/mobile/integration/migrations end-to-end ở commit này: **Chưa xác minh**.

## 7. Git và review

- Bắt đầu bằng status/diff; tách thay đổi theo yêu cầu. Nếu tạo nhánh mới theo quy ước làm việc, dùng `codex/<mo-ta-ngan>` trừ khi người dùng yêu cầu khác. Hiện checkout là main, không tự đổi nhánh chỉ để lập tài liệu.
- Trước commit: `git diff --check`, review đúng file, chạy test liên quan, kiểm không có secret, log, DB dump, dữ liệu người bệnh hay output publish bị thêm vào.
- Chỉ stage file thuộc phạm vi; mô tả trigger/hành vi sau sửa và test đã chạy, ghi rõ test không chạy/lỗi baseline. Không blanket `git add .` nếu có thay đổi khác.
- Không reset/hard clean, rewrite history, push hoặc commit khi chưa được giao. Lần bàn giao này **không tự commit**.
- Commit docs đi kèm tính năng thay đổi, không để các tài liệu mô tả phiên bản trước như thể còn đúng.

## 8. Triển khai và tiếp quản vận hành

Các tài liệu đã tồn tại: [IIS + SQL Express](HUONG-DAN-IIS-SQLEXPRESS-LAN-NETBIRD.md), [HTTP LAN/NetBird](HUONG-DAN-HTTP-LAN-NETBIRD.md), [Windows 11 deployment](DEPLOY-WINDOWS11-NETBIRD.md). Chúng mô tả những phương án khác nhau, không xác nhận phương án đang chạy.

Có thể publish khi được giao bản phát hành:

```powershell
dotnet publish ClinicManagementSystem.Web -c Release -o .\artifacts\publish\LamThaoClinic
```

Hướng dẫn HTTP cũ còn có phương án win-x64 self-contained. Chọn runtime/framework-dependent hoặc self-contained theo máy đích; không suy ra từ việc có EXE. Copy **toàn bộ** output, gồm wwwroot, Data/vietnam-addresses.json, cấu hình cần thiết và file launcher phù hợp; không chỉ thay DLL/EXE lẻ.

Trước triển khai cần xác nhận: SQL instance/catalog/quyền service account; bản backup và restore thử; commit/migration hiện tại; downtime và cách dừng restart loop; chứng chỉ/binding/firewall/LAN/NetBird; runtime/Hosting Bundle nếu IIS; thư mục ghi Logs và khóa Data Protection của cookie; seed Admin bootstrap; cách rollback app tương thích schema.

Không ghi đè cấu hình/secret riêng máy bằng appsettings rỗng trong source. HTTP nội bộ trong tài liệu cũ không là bằng chứng đã bảo vệ mạng; chính sách triển khai thật: **Chưa xác minh**. Logo lấy ảnh ngoài; catalog địa chỉ dùng file đóng gói, không gọi API địa chỉ khi chạy.

Thiếu thông tin cần chủ dự án cung cấp khi quay lại: máy chủ/cách chạy thực, commit và migrations đang áp dụng, quyền truy cập quản trị, quy trình backup/restore, tài khoản thử/người nghiệm thu, bộ file import đã ẩn danh và yêu cầu roadmap. Không đoán hoặc ghi bí mật vào bộ tài liệu này.

## Kiểm tra bổ sung 27/10/2026 — Lưu trữ sản phẩm

`StockCountTests` mở rộng kiểm tra mặc định Active/ACTIVE và Lưu trữ Inactive/INACTIVE, tồn dương khi kiểm kho lưu trữ, kích hoạt/audit và lịch sử. Chạy bằng `dotnet run --project tests/PatientImport -c Release --no-restore -- --stock-count` chỉ trên LocalDB thử nghiệm; harness sinh DB CodexStockCountTest_<GUID>, cleanup có kiểm tra tên. Lượt này biên dịch được nhưng tích hợp bị chặn bởi LocalDB lỗi 50/automatic instance; không đổi sang DB ứng dụng để vượt lỗi. Build Release, 19 Node test picker/pricing và nhánh --pricing qua; chi tiết tại FEATURE_INVENTORY. Tính năng không cần migration. Kiểm tra browser/HTTP và hồi quy DB hóa đơn cũ/sửa/sao chép: **Chưa xác minh**.

## Kiểm tra doanh thu theo ngày (27/10/2026)

`dotnet run --project tests/PatientImport -c Release --no-restore -- --revenue-day` là nhánh dừng sớm không DB: test ranh giới ngày/năm nhuận/chuyển năm, trạng thái hóa đơn, decimal, paging, dịch SQL và controller với spy. Lượt bổ sung đã chạy qua cùng web Release build; vẫn có CS8602 baseline/NU1900 feed. Kiểm chứng SQL Server/browser/HTTP: **Chưa xác minh**. Không cần migration. Chi tiết tại FEATURE_INVENTORY.
