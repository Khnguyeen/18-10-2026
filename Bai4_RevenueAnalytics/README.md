# Bài 4 - Thống kê doanh thu bất đồng bộ

- Mở `Bai4_RevenueAnalytics.csproj` bằng Visual Studio 2026 và nhấn F5.
- Báo cáo mô phỏng 100 giao dịch, chạy trong background bằng `Task.Run`, dùng `async/await`, `Progress<T>` và `CancellationTokenSource`.
- Có bộ lọc ngày/chi nhánh, DataGridView, tiến độ, hủy tác vụ an toàn. Dữ liệu chỉ là dữ liệu mô phỏng, chưa kết nối CSDL thật.
