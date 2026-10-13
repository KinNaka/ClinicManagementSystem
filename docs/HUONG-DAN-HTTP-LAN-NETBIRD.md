# Chạy Phòng Khám Lâm Thảo qua HTTP trong LAN và NetBird

Hướng dẫn này dùng **file publish chạy trực tiếp**, **SQL Express**, **HTTP cổng 5000**. Không cần IIS, chứng chỉ HTTPS hoặc Windows Service.

Máy chủ phải luôn bật và giữ ứng dụng chạy. HTTP trong LAN không mã hóa mật khẩu/dữ liệu trên đường truyền; chỉ dùng trên mạng nội bộ được quản lý. NetBird mã hóa tunnel từ xa. Không mở cổng 5000 ra Internet bằng port forwarding.

## Bước 1 Chuẩn bị database trên máy chủ

1. Cài SQL Server Express và SSMS nếu chưa có. Hướng dẫn dùng instance **SQLEXPRESS**.
2. Mở SSMS → Server name **localhost\SQLEXPRESS** → Windows Authentication → Connect.
3. Chọn File → Open → File, mở **Tao-Database.sql** trong thư mục publish được cung cấp.
4. Chạy script bằng tài khoản Windows có quyền tạo/cập nhật database. Script chỉ tạo/cập nhật cấu trúc; chưa nhập khách hàng, hàng hóa hoặc hóa đơn.
5. Refresh Databases: phải có **ClinicManagementSystem**, các bảng Patients, Products, Sales, AspNetUsers và AspNetRoles.

Ứng dụng chạy dưới tài khoản Windows mở file chạy. Nếu SSMS dùng tài khoản Windows khác với người chạy ứng dụng, cấp quyền đọc/ghi database cho tài khoản chạy ứng dụng. Không cần mở cổng SQL cho các máy nhân viên.

## Bước 2 Chép bản publish

Bản publish mới nằm tại:

```text
C:\Users\Kin\Desktop\Project PK\ClinicManagementSystem\artifacts\publish\LamThaoClinic
```

Chép **toàn bộ nội dung** sang một thư mục cố định trên máy chủ, ví dụ:

```text
C:\LamThaoClinic
```

Không chỉ chép riêng `.exe`. Thư mục cần có **Chay-PhongKham.cmd**, `.exe`, DLL, **appsettings.json**, **appsettings.LocalNetwork.json**, **wwwroot** và **Data**.

Nếu cập nhật source sau bản publish này, publish lại:

```powershell
dotnet publish .\ClinicManagementSystem.Web -c Release -r win-x64 --self-contained true -o .\artifacts\publish\LamThaoClinic
```

Nếu tự publish lại, có thể tạo script database từ source bằng:

```powershell
dotnet ef migrations script --idempotent --project .\ClinicManagementSystem.Web -o .\database.sql
```

Script tự tạo theo lệnh này không tự tạo/chọn database: trong SSMS, tạo ClinicManagementSystem nếu chưa có, chọn database đó rồi chạy script. File **Tao-Database.sql** cung cấp sẵn đã có phần tạo/chọn database ở đầu.

## Bước 3 Cấu hình và tạo Admin lần đầu

Mở **appsettings.json** trong thư mục triển khai. Connection string đã được chuẩn bị:

```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ClinicManagementSystem;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

Mở **appsettings.LocalNetwork.json**. Giữ Hosting và Urls, chỉ điền email/mật khẩu của bạn vào SeedAdmin:

```json
{
  "Hosting": {
    "AllowHttp": true
  },
  "Urls": "http://0.0.0.0:5000",
  "SeedAdmin": {
    "Email": "EMAIL_ADMIN_CUA_BAN",
    "Password": "MAT_KHAU_ADMIN_CUA_BAN"
  }
}
```

Thay hai chữ mẫu bằng giá trị thực tế. Mật khẩu tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Nếu đã có Admin thì có thể giữ Email và Password rỗng.

Bật File Explorer → View → Show → File name extensions để file không bị lưu thành `.json.txt`. Không đưa cấu hình chứa mật khẩu thật lên Git.

## Bước 4 Chạy và kiểm tra ngay trên máy chủ

1. Bấm đúp **Chay-PhongKham.cmd**.
2. Giữ cửa sổ đang chạy, không đóng nó.
3. Khi thấy **Now listening on: http://0.0.0.0:5000**, mở:

```text
http://localhost:5000
```

4. Đăng nhập Admin và kiểm tra trang chính.
5. Đăng nhập thành công rồi để Email và Password trong SeedAdmin thành chuỗi rỗng `""`, lưu file.
6. Khi cần dừng ứng dụng, nhấn **Ctrl+C** trong cửa sổ chạy. Bấm file `.cmd` để chạy lại.

File `.cmd` chọn môi trường **LocalNetwork**. Ở môi trường này ứng dụng đã được chỉnh để đăng nhập qua HTTP và không chuyển hướng sang HTTPS. Các môi trường không bật Hosting:AllowHttp vẫn giữ yêu cầu HTTPS.

Nếu trình duyệt cố chuyển localhost sang HTTPS do truy cập trước đây, kiểm tra thanh địa chỉ, dùng cửa sổ InPrivate/Incognito hoặc xóa chính sách HSTS của địa chỉ cũ. Truy cập bằng IP LAN của máy chủ cũng cần dùng đúng `http://`.

## Bước 5 Ghi lại IP LAN và IP NetBird

Trên máy chủ chạy:

```powershell
ipconfig
```

Ghi IPv4 của adapter Ethernet/Wi-Fi đang kết nối LAN, ví dụ **192.168.1.50**. Nên đặt DHCP reservation trên router để IP ổn định.

Cài NetBird trên máy chủ, Connect và đăng nhập vào mạng NetBird của phòng khám. Trong Dashboard → Peers, ghi **IP NetBird của máy chủ**, ví dụ **100.90.10.20**. Ví dụ ở đây không phải IP thật của bạn.

## Bước 6 Mở Windows Firewall đúng phạm vi

Mở PowerShell **Run as administrator** trên máy chủ.

### Cho phép các máy cùng LAN

```powershell
$clinicLanIp = Read-Host "Nhap IP LAN may chu"
New-NetFirewallRule -DisplayName "Lam Thao HTTP LAN 5000" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5000 -LocalAddress $clinicLanIp -RemoteAddress LocalSubnet -Profile Any
```

Rule này dành cho cùng subnet trực tiếp. Nếu LAN có nhiều subnet/VLAN, bổ sung đúng subnet được phép vào RemoteAddress.

### Cho phép máy từ xa qua NetBird

Sau khi đăng ký các máy truy cập vào NetBird, lấy IP NetBird của từng máy từ Peers và chạy:

```powershell
$clinicVpnIp = Read-Host "Nhap IP NetBird may chu"
$clinicRemoteIps = (Read-Host "Nhap cac IP NetBird may truy cap, cach nhau bang dau phay").Split(',') | ForEach-Object { $_.Trim() }
New-NetFirewallRule -DisplayName "Lam Thao HTTP NetBird 5000" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5000 -LocalAddress $clinicVpnIp -RemoteAddress $clinicRemoteIps -Profile Any
```

Không tắt Windows Firewall. Khi thêm máy từ xa mới, cập nhật phạm vi RemoteAddress của rule NetBird.

## Bước 7 Dùng trên các máy cùng LAN

Các máy nhân viên không cần cài ứng dụng, SQL Server, .NET hoặc chứng chỉ.

1. Kết nối cùng LAN với máy chủ.
2. Mở Chrome/Edge.
3. Nhập **IP LAN thật của máy chủ**, ví dụ:

```text
http://192.168.1.50:5000
```

4. Đăng nhập tài khoản ứng dụng.

**Không dùng localhost trên máy nhân viên.** localhost luôn là chính máy đang mở trình duyệt. `0.0.0.0` là địa chỉ lắng nghe, không phải URL để nhập vào trình duyệt.

Kiểm tra đường kết nối bằng:

```powershell
Test-NetConnection IP_LAN_MAY_CHU -Port 5000
```

Thay IP_LAN_MAY_CHU bằng IP thật. TcpTestSucceeded phải là True.

## Bước 8 Dùng từ xa qua NetBird

1. Cài NetBird trên máy từ xa và đăng nhập tài khoản được phép tham gia mạng phòng khám.
2. Trên NetBird Dashboard, tạo peer group **clinic-server** chứa máy chủ và **clinic-users** chứa các máy truy cập.
3. Tạo policy từ **clinic-users → clinic-server**, TCP, port **5000**.
4. Rà lại policy All-to-All mặc định; thu hẹp/tắt policy rộng nếu nó làm mất tác dụng giới hạn vừa đặt.
5. Bảo đảm firewall máy chủ đã cho phép IP NetBird của máy từ xa theo bước 6.
6. Khi cả hai peer Online, mở **IP NetBird thật của máy chủ**, ví dụ:

```text
http://100.90.10.20:5000
```

7. Đăng nhập bằng tài khoản ứng dụng. Tài khoản NetBird và tài khoản ứng dụng là hai tài khoản riêng.

Kiểm tra đường kết nối từ máy từ xa:

```powershell
Test-NetConnection IP_NETBIRD_MAY_CHU -Port 5000
```

Thay IP_NETBIRD_MAY_CHU bằng IP thật. Không cần forward port trên router.

## Bước 9 Nhập dữ liệu và sử dụng hằng ngày

Trong ứng dụng, nhập **khách hàng → hàng hóa → hóa đơn KiotViet** theo thứ tự để liên kết mã. Luôn xem trước trước khi nhập. Sau đó tạo tài khoản Staff cho từng nhân viên.

- Máy chủ không được Sleep khi đang sử dụng.
- Sau khi khởi động lại máy chủ, mở lại **Chay-PhongKham.cmd**. Cách này chưa tự chạy như Windows Service.
- Khi đóng ứng dụng hoặc tắt máy chủ, các máy khác không truy cập được.
- Backup database ClinicManagementSystem trong SSMS sau khi nhập dữ liệu và trước khi cập nhật.
- Khi cập nhật: dừng ứng dụng, backup SQL, chép bản publish mới và giữ cấu hình LocalNetwork của máy chủ, cập nhật schema nếu có migration mới, chạy lại.

## Khi gặp lỗi

| Lỗi | Kiểm tra |
|---|---|
| Không chạy được / cửa sổ hiện lỗi SQL | SQL Express đang chạy, đúng instance SQLEXPRESS, database đã có bảng |
| Invalid object name AspNetRoles | Chưa chạy Tao-Database.sql trên đúng instance/database |
| Không có Admin | Điền SeedAdmin trong appsettings.LocalNetwork.json và chạy lại |
| Đăng nhập bị chuyển HTTPS hoặc không giữ đăng nhập | Chạy đúng file Chay-PhongKham.cmd, dùng http:// và môi trường LocalNetwork |
| Address already in use | Cổng 5000 đang được ứng dụng khác dùng; dừng đúng bản chạy cũ hoặc đổi cổng trong cấu hình và các rule |
| LAN không truy cập được | Máy chủ đang chạy, đúng IP/cổng 5000, không dùng guest Wi-Fi chặn máy nội bộ, firewall đúng |
| NetBird không truy cập được | Hai peer Online, policy TCP 5000 và firewall cho IP máy khách |

Nguồn tham khảo: [Microsoft Kestrel endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0), [NetBird Windows](https://docs.netbird.io/get-started/install/windows), [NetBird policies](https://docs.netbird.io/manage/access-control/manage-network-access).
