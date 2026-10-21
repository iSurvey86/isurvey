# Trợ lý AI iSurvey — Operator Civil/CAD (Admin-Only)

## Mục tiêu
Chat trong Civil → **Claude** → **thao tác trực tiếp** trên bản vẽ (như người gõ Command Line), không chỉ tư vấn lệnh.

Không cần Node.js cho đường dùng mặc định. MCP ngoài là **tùy chọn**.

## Cấu hình `%AppData%\iSurvey\ai.config.json`

```json
{
  "Provider": "Claude",
  "ApiKey": "sk-ant-...",
  "Model": "claude-sonnet-5-5",
  "McpServers": [
    {
      "Name": "filesystem",
      "Enabled": false,
      "Command": "npx.cmd",
      "Args": [ "-y", "@modelcontextprotocol/server-filesystem", "%USERPROFILE%" ]
    }
  ]
}
```

1. Key: [Anthropic Console](https://console.anthropic.com/) → API keys (`sk-ant-api…` hoặc `sk-ant-usr…`).  
2. Model khuyến nghị: `claude-sonnet-5-5` (liệt kê bằng `GET /v1/models` nếu 404).  
3. MCP `Enabled: false` mặc định — **không cần Node**. Bật chỉ khi muốn MCP ngoài.

## Kiến trúc tool

| Tool | Vai trò |
|---|---|
| **`isurvey_execute_cad`** | **Kênh chính** — chuỗi Command Line bất kỳ (`RECTANG`, `CIRCLE`, `POLYGON`, `PLINE`, `ZOOM`…). Không cần tool riêng từng hình. |
| `isurvey_draw_*` | Shortcut API (HCN/line/circle/polyline) — tiện, không bắt buộc. |
| `isurvey_get_drawing_info` / `list_layers` / `get_selection_summary` | Đọc trạng thái bản vẽ. |
| `isurvey_run_isurvey_command` | Lệnh iSurvey whitelist (GOC, D2C…). |
| `isurvey_list_directory` / `read_text_file` | File trong sandbox `%USERPROFILE%` (không Node). |
| `mcp__*` | MCP ngoài (nếu bật). |

Blocklist `execute_cad`: `SHELL`, `NETLOAD`, `QUIT`… — lệnh phá hủy (`ERASE`…) yêu cầu xác nhận.

## Dùng
1. Đăng nhập **Admin** → Ribbon **Trợ Lý AI** hoặc lệnh `AIA`.  
2. Status: `Admin · Claude · sẵn sàng…`.  
3. Ra lệnh tiếng Việt, ví dụ: «vẽ HCN 0,0–20,30», «vẽ lục giác», «zoom extents».

Sau khi cài DLL mới: **đóng hết AutoCAD/Civil rồi mở lại**.

## Build / cài
```powershell
.\deploy\build-bundle.ps1 -Install
```

## Lưu ý
- Chỉ Admin. Không commit ApiKey.  
- Gemini vẫn fallback nếu `Provider` khác Claude (không đủ MCP).  
- MCP qua `npx` cần Node + `npx.cmd` (không dùng file `npx` không đuôi trên Windows).
