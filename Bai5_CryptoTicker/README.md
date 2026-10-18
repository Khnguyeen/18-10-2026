# Bài 5 - Crypto ticker và Thread Safety

- Mở `Bai5_CryptoTicker.csproj` trong Visual Studio 2026 và nhấn F5.
- Bắt đầu giám sát sẽ chạy vòng lặp nền, cập nhật giá BTC/USD giả lập mỗi 2 giây, tô xanh khi tăng/đỏ khi giảm và ghi lịch sử.
- UI được cập nhật qua `InvokeRequired`/`Invoke`, tránh lỗi cross-thread. Nút Tạm dừng hủy vòng lặp an toàn.
- Giá là dữ liệu ngẫu nhiên phục vụ thực hành, không phải tỷ giá thị trường trực tiếp.
