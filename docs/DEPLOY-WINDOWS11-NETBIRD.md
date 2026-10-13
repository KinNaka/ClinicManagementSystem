# Triển khai Phòng Khám Lâm Thảo trên Windows 11 và NetBird

Mô hình: máy truy cập có NetBird → HTTPS trên IIS của máy Windows 11 → SQL Server Express trên cùng máy chủ.

Ứng dụng hiện tại dùng .NET 10, database `ClinicManagementSystem` và đăng nhập bằng ASP.NET Core Identity. Các bước dưới đây là hướng dẫn, chưa cài đặt hoặc thay đổi cấu hình máy của bạn.

## 1. Cài phần mềm trên máy chủ

- Bật **Internet Information Services** và **Web Management Tools → IIS Management Console** trong “Turn Windows features on or off”.
- Sau đó cài **.NET 10 Hosting Bundle**, chọn Windows Hosting Bundle ở [trang .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). Nếu đã cài bundle trước IIS, chạy Repair sau khi bật IIS.
- Cài SQL Server Express và SSMS. Nếu đã có `SQLEXPRESS`, kiểm tra phiên bản trước khi chuyển dữ liệu.
- Cài [NetBird cho Windows](https://docs.netbird.io/get-started/install/windows), đăng ký máy chủ và các máy truy cập vào cùng mạng NetBird. Mỗi nhân viên nên có tài khoản NetBird riêng.

Hướng dẫn IIS chính thức: [Microsoft](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0).

## 2. Chuyển dữ liệu hiện có sang SQL Server Express

LocalDB hiện tại của bạn là phiên bản **17.x**. Backup của phiên bản này không khôi phục trực tiếp được vào SQL Server 2022 **16.x**. Instance đích cần phiên bản tương thích, ví dụ SQL Server 2025 Express **17.x**. Kiểm tra trên cả hai kết nối bằng:

```sql
SELECT @@VERSION;
```

Microsoft xác nhận không khôi phục backup từ bản SQL Server mới xuống bản cũ: [Backup/restore](https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/backup-restore/backup-restore-operations).

Nếu đã có dữ liệu cần giữ:

1. Dừng nhập liệu và dừng phiên F5.
2. Kết nối LocalDB bằng địa chỉ **Instance pipe name** lấy từ `sqllocaldb info mssqllocaldb`.
3. Trong SSMS: `ClinicManagementSystem → Tasks → Back Up → Full`. Lưu `.bak` vào thư mục backup riêng, không nằm trong thư mục website.
4. Kết nối `localhost\SQLEXPRESS` bằng Windows Authentication.
5. `Databases → Restore Database → Device`, chọn `.bak`. SQL Server phải có quyền đọc file backup. Có thể dùng thư mục Backup của instance đích.
6. Chỉ khôi phục vào database đích mới/chưa có dữ liệu cần giữ. Không chọn ghi đè database đang chứa dữ liệu khác.
7. Kiểm tra các bảng Patients, Products, Sales và AspNetUsers trên instance đích. Giữ bản LocalDB và file backup để đối chiếu; không xóa nguồn sau khi chuyển.

Nếu chưa có dữ liệu cần giữ, bỏ bước restore và tạo schema bằng migration ở bước tiếp theo.

Trên máy có source và .NET SDK, chạy từ thư mục solution bằng tài khoản Windows có quyền quản trị database đích:

```powershell
dotnet ef database update --project .\ClinicManagementSystem.Web --connection "Server=localhost\SQLEXPRESS;Database=ClinicManagementSystem;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

Ứng dụng không tự chạy migration khi khởi động, nên bước này cần thực hiện trước khi chạy website.

## 3. Publish và tạo website IIS

Trong Visual Studio: chuột phải dự án **ClinicManagementSystem.Web → Publish → Folder → Release**. Hoặc chạy:

```powershell
dotnet publish .\ClinicManagementSystem.Web -c Release -o .\publish\LamThaoClinic
```

Chép toàn bộ kết quả publish sang `C:\inetpub\LamThaoClinic`, gồm `web.config`, DLL, wwwroot và `Data\vietnam-addresses.json`.

Trong IIS Manager:

- Tạo Application Pool **LamThaoClinic**, `.NET CLR Version: No Managed Code`, Identity: **ApplicationPoolIdentity**.
- Advanced Settings: **Load User Profile = True** để giữ khóa bảo vệ cookie; **Idle Time-out = 0** nếu muốn tránh dừng do không có truy cập.
- Tạo website **LamThaoClinic**, thư mục trên và pool vừa tạo. Cấu hình binding HTTPS ở bước 5.
- Cấp quyền **Read & execute** thư mục website cho `IIS AppPool\LamThaoClinic` qua Properties → Security.

Tên Application Pool phải khớp với tài khoản được cấp quyền SQL dưới đây.

## 4. Cấu hình database và quyền chạy ứng dụng

Tạo `C:\inetpub\LamThaoClinic\appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ClinicManagementSystem;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "AllowedHosts": "lamthao.internal"
}
```

IIS chạy môi trường Production theo mặc định; bảo đảm không đặt `ASPNETCORE_ENVIRONMENT=Development` trong cấu hình triển khai. Windows Authentication ở đây là tài khoản Application Pool, không phải tài khoản Windows của bạn.

Trong SSMS kết nối **SQLEXPRESS**, chạy bằng tài khoản quản trị SQL. Các lệnh tạo login/user chỉ chạy lần đầu; nếu đã có thì bỏ qua phần tương ứng:

```sql
USE master;
CREATE LOGIN [IIS APPPOOL\LamThaoClinic] FROM WINDOWS;
GO
USE ClinicManagementSystem;
CREATE USER [IIS APPPOOL\LamThaoClinic]
FOR LOGIN [IIS APPPOOL\LamThaoClinic];
ALTER ROLE db_datareader ADD MEMBER [IIS APPPOOL\LamThaoClinic];
ALTER ROLE db_datawriter ADD MEMBER [IIS APPPOOL\LamThaoClinic];
```

Không cần cấp `sysadmin` cho website. Migration chạy riêng bằng tài khoản quản trị; website chỉ đọc/ghi dữ liệu.

Nếu restore database có tài khoản Admin, dùng tài khoản đó. Nếu database mới chưa có Admin, thêm tạm cấu hình này vào file Production trên máy chủ:

```json
"SeedAdmin": {
  "Email": "EMAIL_ADMIN_CUA_BAN",
  "Password": "MAT_KHAU_MANH_CUA_BAN"
}
```

Ghép mục này vào cùng đối tượng JSON, có dấu phẩy đúng. Thay các giá trị mẫu bằng email và mật khẩu thực tế; mật khẩu cần ít nhất 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt. Website sẽ tạo Admin khi chạy. Đăng nhập thành công rồi xóa mục SeedAdmin và recycle pool. Không commit mật khẩu hoặc file cấu hình chứa mật khẩu lên Git.

## 5. Thiết lập HTTPS

Ứng dụng đặt cookie `SecurePolicy.Always`, nên phải dùng HTTPS để đăng nhập ổn định.

Ví dụ dùng tên nội bộ **lamthao.internal**. Mở PowerShell **Run as administrator** trên máy chủ:

```powershell
$clinicCertificate = New-SelfSignedCertificate -DnsName "lamthao.internal" -CertStoreLocation "Cert:\LocalMachine\My" -FriendlyName "Lam Thao Internal HTTPS" -NotAfter (Get-Date).AddYears(2)
Export-Certificate -Cert $clinicCertificate -FilePath "$env:TEMP\LamThaoClinic.cer"
```

Trong IIS → website → Bindings → Add:

- Type: **https**, Port: **443**, IP: **All Unassigned**.
- Host name: **lamthao.internal**, bật SNI.
- SSL certificate: **Lam Thao Internal HTTPS**.

Trên mỗi máy Windows truy cập, cài file `.cer` do chính máy chủ tạo vào **Trusted Root Certification Authorities** bằng Certificate Import Wizard. Chỉ cài chứng chỉ của máy chủ bạn quản lý. Đặt lịch thay chứng chỉ trước ngày hết hạn; không cần chia sẻ khóa riêng/PFX.

Thêm dòng sau vào file `C:\Windows\System32\drivers\etc\hosts` trên từng máy truy cập, mở trình soạn thảo bằng quyền Administrator:

```text
IP_NETBIRD_MAY_CHU lamthao.internal
```

Thay `IP_NETBIRD_MAY_CHU` bằng IP thật lấy từ NetBird Dashboard. Ví dụ IP `100.x.x.x` chỉ là ví dụ, không dùng nguyên văn. Trên máy chủ có thể dùng `127.0.0.1 lamthao.internal` để kiểm tra tại chỗ.

Nếu dùng điện thoại, cấu hình tên DNS nội bộ tương ứng trong NetBird và cách tin cậy chứng chỉ theo hệ điều hành; hướng dẫn hosts ở trên dành cho máy Windows.

## 6. Cấp truy cập NetBird và mở firewall

Trong NetBird Dashboard:

1. Tạo peer group **clinic-server**, đưa máy chủ vào nhóm.
2. Tạo peer group **clinic-users**, đưa các máy được phép sử dụng vào nhóm.
3. Tạo policy **clinic-users → clinic-server**, protocol **TCP**, port **443**, hướng truy cập từ máy người dùng tới máy chủ.
4. Rà policy mặc định cho phép mọi peer với nhau; tắt hoặc thu hẹp policy rộng nếu nó làm mất tác dụng giới hạn vừa tạo.

Tham khảo [NetBird access policies](https://docs.netbird.io/manage/access-control/manage-network-access). Không cần tạo route tới mạng LAN khi NetBird đã cài trực tiếp trên máy chủ và máy truy cập.

Mở PowerShell Administrator trên máy chủ. Điền IP thật vào biến, rồi chạy:

```powershell
$clinicServerIp = "IP_NETBIRD_MAY_CHU"
$clinicClientIps = @("IP_NETBIRD_MAY_TRUY_CAP_1", "IP_NETBIRD_MAY_TRUY_CAP_2")
New-NetFirewallRule -DisplayName "Lam Thao HTTPS via NetBird" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 443 -LocalAddress $clinicServerIp -RemoteAddress $clinicClientIps -Profile Any
```

Khi thêm máy truy cập mới, cập nhật nhóm NetBird và phạm vi firewall. Không tắt Windows Firewall. SQL Server ở cùng máy với website nên không cần mở cổng SQL cho các máy nhân viên. Không cần forward cổng website trên router để truy cập bằng NetBird.

## 7. Kiểm tra sử dụng

1. Trên máy chủ: mở `https://lamthao.internal`, đăng nhập Admin.
2. Trên máy từ xa: kết nối NetBird, kiểm tra:

```powershell
Test-NetConnection lamthao.internal -Port 443
```

3. Mở `https://lamthao.internal` bằng trình duyệt. Không dùng `http://` hoặc URL localhost trên máy từ xa.
4. Kiểm tra đăng nhập Admin/Staff, danh sách bệnh nhân, hàng hóa, hóa đơn và mẫu in.
5. Khởi động lại máy chủ, thử lại khi không mở Visual Studio. Kiểm tra dịch vụ SQL Server (SQLEXPRESS), IIS và NetBird khởi động tự động.
6. Đặt Sleep của máy chủ thành Never khi cắm điện. Máy tắt/ngủ hoặc mất mạng thì máy từ xa không truy cập được.

## 8. Sao lưu và cập nhật

- Sao lưu FULL database bằng SSMS trước khi cập nhật; giữ một bản ở thiết bị khác. SQL Server Express không có SQL Server Agent, nên việc tự động hóa backup cần Task Scheduler hoặc công cụ backup phù hợp.
- Giữ cấu hình Production, chứng chỉ và khóa Data Protection của profile Application Pool; tránh đổi tên/xóa pool khi cập nhật thông thường.
- Khi cập nhật: dừng website, backup SQL, publish bản mới, chép nội dung publish nhưng giữ cấu hình Production của máy chủ, chạy migration nếu có, rồi khởi động và kiểm tra lại.
- File Excel nhập và database là dữ liệu riêng; publish website không tự nhập lại Excel.

## 9. Lỗi thường gặp

| Hiện tượng | Kiểm tra |
|---|---|
| 500.19 / thiếu AspNetCoreModule | IIS và .NET 10 Hosting Bundle, Repair bundle sau khi bật IIS |
| 500.30 / ứng dụng không khởi động | Windows Event Viewer → Application; connection string, schema SQL, cấu hình SeedAdmin |
| Login failed for IIS APPPOOL | Login/user/quyền database phải thuộc đúng pool LamThaoClinic |
| SSMS không thấy dữ liệu | Kết nối SQLEXPRESS đúng máy và database, không nhầm với LocalDB |
| Bấm đăng nhập nhưng không vào | Dùng HTTPS, chứng chỉ được tin cậy, profile/khóa Data Protection |
| Timeout từ xa | NetBird hai máy online, policy TCP 443, firewall, hostname/hosts và IIS binding |
| Chứng chỉ sai tên | Truy cập lamthao.internal đúng tên chứng chỉ, không truy cập bằng IP |

Đây là cấu hình chạy một máy Windows 11 cho nhóm nhỏ. Chưa triển khai thực tế hoặc chuyển dữ liệu thật trong quá trình viết hướng dẫn.
