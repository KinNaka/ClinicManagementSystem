# Deploy Phòng Khám Lâm Thảo trên Windows 11

## Bạn sẽ có gì sau khi làm xong

Một máy Windows 11 chạy cả **website IIS** và **database SQL Server Express**.

- Máy trong phòng khám truy cập bằng `https://IP_LAN_MAY_CHU`.
- Máy ở ngoài phòng khám bật NetBird rồi truy cập `https://IP_NETBIRD_MAY_CHU`.
- Bạn nhập dữ liệu sau bằng chức năng nhập trong ứng dụng. **Không cần chuyển dữ liệu LocalDB cũ.**

Hướng dẫn này dùng thư mục website **C:\inetpub\LamThaoClinic**, tên website và Application Pool đều là **LamThaoClinic**, cổng HTTPS **443**. Làm theo thứ tự; chỉ chuyển bước khi đã thấy kết quả cần kiểm tra.

## Bước 1 Chuẩn bị máy chủ

Trên máy Windows 11 sẽ làm máy chủ:

1. Cài **SQL Server Express**. Nếu chưa cài, lấy bản Express từ [Microsoft](https://www.microsoft.com/en-us/sql-server/sql-server-downloads). Hướng dẫn giả định instance tên **SQLEXPRESS**; nếu cài instance tên khác thì phải đổi tên tương ứng trong connection string.
2. Cài **SQL Server Management Studio**, gọi tắt **SSMS**.
3. Mở Start, tìm **Turn Windows features on or off**.
4. Bật **Internet Information Services**, gồm **Web Management Tools → IIS Management Console** và **World Wide Web Services**.
5. Sau khi IIS cài xong, cài **.NET 10 Hosting Bundle** tại [trang tải .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0). Chọn Windows **Hosting Bundle**. Nếu đã cài bundle trước khi bật IIS, chạy lại installer và chọn Repair.
6. Khởi động lại máy nếu Windows yêu cầu.

**Kết quả cần thấy:** mở được IIS Manager bằng cách nhấn Windows + R, nhập `inetmgr`. Mở SSMS, kết nối `localhost\SQLEXPRESS` bằng **Windows Authentication** thành công.

Máy dùng để publish source cần Visual Studio hỗ trợ .NET 10 hoặc .NET 10 SDK. Máy đó có thể chính là máy chủ này.

## Bước 2 Ghi lại địa chỉ máy chủ

### Địa chỉ LAN

Trên máy chủ, mở PowerShell và chạy:

```powershell
ipconfig
```

Tìm **IPv4 Address** của adapter Ethernet/Wi-Fi đang dùng, ví dụ `192.168.1.50`. Ghi lại. Nên đặt DHCP reservation cho máy chủ trên router để địa chỉ này ổn định. Không dùng địa chỉ adapter đang ngắt kết nối.

### Địa chỉ NetBird

1. Tạo tài khoản/quản lý mạng tại [NetBird](https://app.netbird.io).
2. Cài [NetBird Windows client](https://docs.netbird.io/get-started/install/windows) trên máy chủ.
3. Mở NetBird, chọn Connect và đăng nhập.
4. Trong NetBird Dashboard → Peers, tìm máy chủ và ghi lại **NetBird IP** của nó.

**Kết quả cần thấy:** máy chủ Online trong NetBird; bạn có hai IP riêng: IP LAN và IP NetBird. Các IP ví dụ trong tài liệu không phải IP thật của bạn.

## Bước 3 Tạo cấu trúc database mới

Database chưa có dữ liệu khách hàng/hàng hóa vẫn cần đủ bảng để website khởi động.

Trên máy có source, mở terminal tại thư mục solution, nơi chứa thư mục `ClinicManagementSystem.Web`. Nếu SQL nằm trên máy khác, thực hiện bước này trên máy chủ có source để lệnh dưới đây kết nối đúng máy.

```powershell
dotnet ef database update --project .\ClinicManagementSystem.Web --connection "Server=localhost\SQLEXPRESS;Database=ClinicManagementSystem;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

Nếu báo không tìm thấy `dotnet-ef`, cài công cụ rồi chạy lại lệnh trên:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.11
```

Nếu công cụ đã có thì không cần cài lại. Tài khoản Windows chạy migration cần quyền tạo/cập nhật database trong SQL Express. Tài khoản được chọn làm SQL administrator lúc cài SQL thường dùng để thực hiện bước này.

**Kết quả cần thấy:** trong SSMS → Databases → Refresh có **ClinicManagementSystem**; bên dưới Tables có các bảng **Patients, Products, Sales, AspNetUsers, AspNetRoles**. Các bảng nghiệp vụ chưa có dữ liệu là bình thường.

## Bước 4 Publish ứng dụng

### Cách dùng Visual Studio

1. Chuột phải dự án **ClinicManagementSystem.Web** → **Publish**.
2. Chọn **Folder**.
3. Configuration: **Release**.
4. Target runtime: **win-x64**. Deployment mode có thể chọn **Self-contained**.
5. Chọn thư mục output rồi bấm **Publish**.

### Hoặc dùng lệnh

Chạy tại thư mục solution:

```powershell
dotnet publish .\ClinicManagementSystem.Web -c Release -r win-x64 --self-contained true -o .\artifacts\publish\LamThaoClinic
```

Dự án của bạn đã có một bản publish ở:

```text
C:\Users\Kin\Desktop\Project PK\ClinicManagementSystem\artifacts\publish\LamThaoClinic
```

Nếu đã sửa source sau bản publish đó, publish lại trước khi triển khai.

Trên máy chủ, tạo thư mục:

```text
C:\inetpub\LamThaoClinic
```

Chép **toàn bộ nội dung thư mục publish** vào đó. Không chỉ chép riêng file `.exe`.

**Kết quả cần thấy:** trực tiếp trong `C:\inetpub\LamThaoClinic` có `web.config`, `ClinicManagementSystem.Web.dll`, `appsettings.json`, thư mục `wwwroot` và `Data\vietnam-addresses.json`.

## Bước 5 Tạo website trong IIS

1. Nhấn Windows + R → nhập `inetmgr`.
2. Chọn **Application Pools → Add Application Pool**.
3. Name: **LamThaoClinic**.
4. .NET CLR version: **No Managed Code**.
5. Managed pipeline mode: **Integrated**.
6. Chuột phải pool vừa tạo → **Advanced Settings**:
   - Identity: **ApplicationPoolIdentity**.
   - Load User Profile: **True**, để khóa bảo vệ đăng nhập được giữ trong profile.
   - Idle Time-out: **0** nếu muốn tránh pool dừng vì không có truy cập.
7. Chuột phải **Sites → Add Website**:
   - Site name: **LamThaoClinic**.
   - Application pool: **LamThaoClinic**.
   - Physical path: **C:\inetpub\LamThaoClinic**.
   - Binding tạm: **http**, IP All Unassigned, Port **8080**, Host name để trống.

Binding tạm chỉ để tạo site. Chưa dùng nó để đăng nhập; bước sau sẽ thêm HTTPS.

### Cấp quyền đọc thư mục

Mở PowerShell **Run as administrator** và chạy:

```powershell
icacls C:\inetpub\LamThaoClinic /grant "IIS AppPool\LamThaoClinic:(OI)(CI)RX"
```

**Kết quả cần thấy:** website và pool tên LamThaoClinic xuất hiện trong IIS. Tên pool phải đúng vì bước SQL tiếp theo dùng tên này.

## Bước 6 Cấp quyền SQL và tạo Admin

### Cấp quyền database cho website

Mở SSMS → kết nối **localhost\SQLEXPRESS** bằng tài khoản quản trị SQL → New Query → chạy:

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

Lệnh tạo login/user chỉ chạy lần đầu. Nếu chúng đã tồn tại, bỏ qua lệnh tạo tương ứng. Không cấp sysadmin cho website.

### Tạo cấu hình Production và tài khoản Admin ban đầu

Trong `C:\inetpub\LamThaoClinic`, tạo file **appsettings.Production.json**, nội dung:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ClinicManagementSystem;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "SeedAdmin": {
    "Email": "EMAIL_ADMIN_CUA_BAN",
    "Password": "MAT_KHAU_ADMIN_CUA_BAN"
  }
}
```

Thay **EMAIL_ADMIN_CUA_BAN** bằng email dùng đăng nhập và **MAT_KHAU_ADMIN_CUA_BAN** bằng mật khẩu thật. Mật khẩu tối thiểu 8 ký tự, có chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Không dùng nguyên các chữ mẫu.

Bật File Explorer → View → Show → **File name extensions** để chắc chắn file không bị thành `appsettings.Production.json.txt`.

Ứng dụng sẽ tạo Admin khi chạy lần đầu. Sau khi đăng nhập thành công ở bước 9, xóa mục **SeedAdmin** khỏi file, giữ lại ConnectionStrings và JSON hợp lệ. Không đưa file chứa mật khẩu thật lên Git.

IIS mặc định chạy Production. Bảo đảm `web.config` không đặt `ASPNETCORE_ENVIRONMENT` thành Development.

## Bước 7 Tạo chứng chỉ HTTPS cho cả LAN và NetBird

Website dùng cookie đăng nhập yêu cầu HTTPS. Chứng chỉ cần bao gồm cả IP LAN và IP NetBird để truy cập bằng IP không bị sai tên.

Trong dự án của bạn có sẵn file:

```text
artifacts\deploy\Create-HttpsCertificate.ps1
```

Chép file này lên máy chủ vào:

```text
C:\inetpub\LamThaoClinic\Create-HttpsCertificate.ps1
```

Mở PowerShell **Run as administrator**, chạy nguyên khối sau. Khi được hỏi, nhập hai IP thật đã ghi ở bước 2:

```powershell
$clinicLanIp = Read-Host "Nhap IP LAN may chu"
$clinicVpnIp = Read-Host "Nhap IP NetBird may chu"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\inetpub\LamThaoClinic\Create-HttpsCertificate.ps1" -LanIp $clinicLanIp -VpnIp $clinicVpnIp
```

Script tạo chứng chỉ HTTPS, cài chứng chỉ gốc tin cậy trên máy chủ và xuất file công khai **Clinic-Root-CA.cer** vào cùng thư mục. Chỉ cần chia sẻ file `.cer` này cho máy truy cập, không chia sẻ khóa riêng.

**Kết quả cần thấy:** mở Windows + R → `certlm.msc` → Personal → Certificates có **Clinic Website HTTPS**; thư mục website có **Clinic-Root-CA.cer**.

Chứng chỉ website do script này tạo có hạn 365 ngày. Cần gia hạn trước khi hết hạn. Nếu tạo lại bằng script, chứng chỉ gốc cũng thay đổi, nên phải cài lại file `.cer` mới trên các máy truy cập.

## Bước 8 Thêm HTTPS và mở quyền truy cập mạng

### Thêm HTTPS vào IIS

1. IIS Manager → Sites → **LamThaoClinic** → **Bindings** → Add.
2. Type: **https**.
3. IP address: **All Unassigned**.
4. Port: **443**.
5. Host name: **để trống**, để truy cập bằng IP.
6. Không bật Require Server Name Indication.
7. SSL certificate: chọn **Clinic Website HTTPS**.
8. OK.
9. Xóa binding HTTP tạm cổng 8080. Địa chỉ sử dụng sẽ là HTTPS.

Nếu cổng 443 đã bị một website khác dùng với binding trùng, xử lý binding đó trước; không dừng/xóa website khác khi chưa biết nó phục vụ việc gì.

### Mở Windows Firewall theo mạng được phép

Trong PowerShell Administrator, nhập IP máy chủ và IP NetBird của các máy được phép truy cập. Với máy truy cập mới chưa đăng ký NetBird, quay lại thực hiện/cập nhật rule này sau bước 10.

```powershell
$clinicLanIp = Read-Host "Nhap IP LAN may chu"
$clinicVpnIp = Read-Host "Nhap IP NetBird may chu"
$clinicRemoteIps = (Read-Host "Nhap cac IP NetBird may truy cap, cach nhau bang dau phay").Split(',') | ForEach-Object { $_.Trim() }

New-NetFirewallRule -DisplayName "Lam Thao HTTPS LAN" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 443 -LocalAddress $clinicLanIp -RemoteAddress LocalSubnet -Profile Any

New-NetFirewallRule -DisplayName "Lam Thao HTTPS NetBird" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 443 -LocalAddress $clinicVpnIp -RemoteAddress $clinicRemoteIps -Profile Any
```

Rule LAN ở trên dành cho cùng subnet trực tiếp của máy chủ. Nếu LAN có nhiều VLAN/subnet, bổ sung đúng subnet được phép vào rule. Không tắt Windows Firewall.

### Giới hạn NetBird

Trong NetBird Dashboard:

1. Tạo nhóm peer **clinic-server**, thêm máy chủ.
2. Tạo nhóm peer **clinic-users**, thêm các máy được phép dùng.
3. Tạo policy từ **clinic-users → clinic-server**, protocol **TCP**, port **443**.
4. Rà policy mặc định cho phép tất cả peer với nhau. Thu hẹp hoặc tắt policy rộng nếu nó làm mất tác dụng giới hạn vừa tạo.

Không cần mở cổng SQL cho nhân viên. Chỉ website trên máy chủ kết nối SQL. Không cần forward cổng 443 trên router để dùng NetBird.

## Bước 9 Kiểm tra trên máy chủ và nhập dữ liệu

1. Trong IIS, Start website và bảo đảm Application Pool đang Started.
2. Mở trình duyệt trên máy chủ:

```text
https://localhost
```

3. Đăng nhập bằng email/mật khẩu Admin ở bước 6.
4. Xóa mục SeedAdmin trong file Production rồi recycle pool.
5. Dùng chức năng nhập dữ liệu của ứng dụng theo thứ tự:
   - Nhập **khách hàng** trước để có mã KH và tổng bán cũ.
   - Nhập **hàng hóa** tiếp theo.
   - Nhập **hóa đơn KiotViet** sau cùng để liên kết theo mã.
6. Luôn xem trước và đối chiếu trước khi bấm nhập. Combo nhập vào cần cấu hình thành phần trước khi bán mới.
7. Tạo tài khoản Staff trong trang Tài khoản.

**Kết quả cần thấy:** đăng nhập được, các trang tải bình thường, dữ liệu nhập xuất hiện trong ứng dụng và trong database ClinicManagementSystem của SQLEXPRESS.

## Bước 10 Truy cập từ máy khác trong LAN

Trên từng máy nhân viên cùng mạng LAN:

1. Chép file **Clinic-Root-CA.cer** từ máy chủ sang máy nhân viên.
2. Bấm đúp file → **Install Certificate**.
3. Chọn **Local Machine**; chấp nhận yêu cầu Administrator nếu có.
4. Chọn **Place all certificates in the following store → Browse → Trusted Root Certification Authorities**.
5. Next → Finish. Chỉ cài chứng chỉ gốc do máy chủ phòng khám của bạn tạo.
6. Mở trình duyệt, nhập địa chỉ IP LAN thật của máy chủ, ví dụ:

```text
https://192.168.1.50
```

7. Đăng nhập tài khoản Admin/Staff của ứng dụng.

Máy nhân viên không cần cài SQL Server, Visual Studio hoặc .NET SDK. Không nhập `localhost` trên máy nhân viên vì đó là chính máy nhân viên, không phải máy chủ.

Nếu cần kiểm tra kết nối, trên máy nhân viên chạy:

```powershell
Test-NetConnection IP_LAN_MAY_CHU -Port 443
```

Thay IP_LAN_MAY_CHU bằng IP thật. **TcpTestSucceeded: True** nghĩa là cổng website tới được.

## Bước 11 Truy cập từ ngoài phòng khám bằng NetBird

Trên máy ở nhà hoặc máy mang đi:

1. Cài NetBird client.
2. Đăng nhập tài khoản NetBird đã được cấp quyền vào cùng mạng phòng khám. Không dùng chung mật khẩu quản trị NetBird cho nhân viên.
3. Trong NetBird Dashboard, thêm peer của máy này vào **clinic-users**.
4. Cập nhật rule firewall NetBird trên máy chủ để bao gồm IP NetBird của máy này.
5. Cài **Clinic-Root-CA.cer** như bước 10 nếu máy chưa cài.
6. Bật Connect trong NetBird. Máy chủ và máy truy cập phải Online.
7. Mở trình duyệt với **IP NetBird của máy chủ**, ví dụ:

```text
https://100.90.10.20
```

Đây là ví dụ; dùng IP thật trong Peers của máy chủ. Đăng nhập bằng tài khoản ứng dụng. Tài khoản NetBird và tài khoản ứng dụng là hai lớp đăng nhập riêng.

Kiểm tra nếu không mở được:

```powershell
Test-NetConnection IP_NETBIRD_MAY_CHU -Port 443
```

Muốn xác nhận đang dùng từ xa thật, thử trên máy ngoài mạng LAN phòng khám, ví dụ dùng Wi-Fi khác hoặc hotspot điện thoại, với NetBird đang kết nối.

## Bước 12 Giữ máy chủ hoạt động và sao lưu

- Máy chủ phải bật và có kết nối mạng. Trong Settings → System → Power, đặt Sleep thành Never khi cắm điện.
- Dịch vụ **SQL Server (SQLEXPRESS)** và **World Wide Web Publishing Service** cần khởi động tự động. Kiểm tra bằng `services.msc`.
- Khởi động lại máy chủ một lần và thử website mà không mở Visual Studio.
- Trong SSMS, backup FULL database **ClinicManagementSystem** sau khi nhập dữ liệu và trước mỗi lần cập nhật. Giữ bản backup ở thiết bị khác, không chỉ trên ổ máy chủ.
- Giữ bản cấu hình Production riêng. Khi cập nhật: Stop website, backup SQL, publish mới, chép bản mới và giữ cấu hình Production, chạy migration nếu có, Start và kiểm tra lại.
- Nếu IP LAN/NetBird của máy chủ đổi, cập nhật firewall và tạo chứng chỉ bao gồm IP mới, sau đó cập nhật binding và chứng chỉ tin cậy trên máy khách khi cần.

## Cách xử lý lỗi thường gặp

| Hiện tượng | Việc cần kiểm tra |
|---|---|
| IIS lỗi 500.19 | Kiểm tra .NET 10 Hosting Bundle và AspNetCoreModule; Repair sau khi bật IIS |
| IIS lỗi 500.30 | Event Viewer → Windows Logs → Application; kiểm tra SQL, migration và SeedAdmin |
| Login failed for IIS APPPOOL | Login/user/quyền SQL phải là IIS APPPOOL\LamThaoClinic trên đúng instance SQLEXPRESS |
| Invalid object name AspNetRoles hoặc Patients | Chưa chạy migration trên đúng database |
| Không có Admin để đăng nhập | Điền SeedAdmin trong Production, recycle pool và xem log nếu tạo thất bại |
| Chứng chỉ không tin cậy | Cài đúng file Clinic-Root-CA.cer mới nhất trên máy truy cập |
| Chứng chỉ sai tên | IP đang truy cập phải được đưa vào chứng chỉ ở bước 7 |
| LAN không vào được | Website Started, IP LAN đúng, cùng mạng, firewall cho phép TCP 443 |
| NetBird không vào được | Cả hai peer Online, policy TCP 443 đúng, firewall có IP máy truy cập |
| Mở localhost trên máy nhân viên không có website | Thay bằng IP LAN hoặc IP NetBird của máy chủ |
| SSMS không thấy dữ liệu vừa nhập | Kết nối localhost\SQLEXPRESS trên máy chủ và mở đúng ClinicManagementSystem |

## Tài liệu chính thức

- [Microsoft hướng dẫn ASP.NET Core trên IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0)
- [Microsoft tải .NET 10 và Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [NetBird cài đặt Windows client](https://docs.netbird.io/get-started/install/windows)
- [NetBird nhóm và access policies](https://docs.netbird.io/manage/access-control/manage-network-access)

Tài liệu viết cho dự án Phòng Khám Lâm Thảo hiện tại. Việc tạo file hướng dẫn không tự cài IIS, mở firewall, tạo database hay nhập dữ liệu thật trên máy của bạn.
