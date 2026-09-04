# Net10 SSE Demo

一个基于 ASP.NET Core .NET 10 Minimal API 的 Server-Sent Events（SSE）示例项目。

项目提供一个持续推送事件的服务端接口，以及一个可以直接在浏览器中查看事件流的监视页面。

## 功能

- 使用 .NET 10 和 ASP.NET Core Minimal API
- 提供 `GET /events` SSE 端点
- 服务端每秒推送一个 `tick` 事件
- 每条事件包含递增编号和 UTC 时间戳
- 客户端断开时自动取消服务端异步事件生成
- 提供浏览器页面查看连接状态、事件数量和事件日志

## 环境要求

- .NET 10 SDK
- 支持 SSE 的现代浏览器

检查 SDK 版本：

```powershell
dotnet --version
```

## 运行项目

在项目根目录执行：

```powershell
dotnet run --urls http://localhost:5080
```

打开浏览器访问：

```text
http://localhost:5080
```

点击“建立连接”后，页面会通过浏览器原生 `EventSource` API 连接 `/events`，并显示持续收到的事件。

## SSE 接口

### 请求

```http
GET /events HTTP/1.1
Accept: text/event-stream
```

### 响应

服务端返回以下响应头：

```http
HTTP/1.1 200 OK
Content-Type: text/event-stream
Cache-Control: no-cache
Connection: keep-alive
```

事件示例：

```text
event: tick
data: {"number":1,"sentAt":"2026-09-04T03:19:51.4174085+00:00"}

```

也可以使用 `curl` 查看事件流：

```powershell
curl.exe --no-buffer http://localhost:5080/events
```

由于 SSE 是持续连接，使用 `Ctrl+C` 停止命令。

## 项目结构

```text
.
├── Program.cs              # Minimal API 和 SSE 端点
├── SseDemo.csproj          # .NET 10 项目配置
├── wwwroot/
│   └── index.html          # SSE 浏览器监视页面
├── Properties/
│   └── launchSettings.json # 本地启动配置
├── appsettings.json        # 应用配置
└── .gitignore              # .NET 项目 Git 忽略规则
```

## 构建

```powershell
dotnet restore
dotnet build
dotnet run
```

## SSE 说明

### 什么是 SSE

SSE（Server-Sent Events）是一种基于 HTTP 的服务器到客户端单向通信方式。客户端建立连接后，服务端可以持续发送事件；客户端无需轮询即可接收更新。

一次 SSE 通信通常包含以下步骤：

1. 浏览器使用 `EventSource` 发起一个长连接请求。
2. 服务端返回 `Content-Type: text/event-stream`。
3. 服务端持续写入事件，并使用空行分隔每条事件。
4. 浏览器解析事件，并触发对应的消息处理函数。

SSE 使用普通 HTTP，不需要 WebSocket 握手，因此通常更容易接入现有的 HTTP 服务、代理和认证体系。

本示例使用以下 SSE 格式：

- `event`：事件名称，本项目为 `tick`
- `data`：事件数据，内容为 JSON
- `id`：事件标识，可用于断线重连后恢复事件位置
- `retry`：客户端重连等待时间，单位为毫秒
- `:`：注释行，常用于发送心跳以保持连接活跃
- 空行：表示一条事件结束

示例：

```text
id: 42
event: message
data: {"content":"hello"}
retry: 5000

```

`data` 可以出现多次，客户端会将多行内容拼接后再交给事件处理器。事件中的 JSON 只是常见约定，并不是 SSE 协议本身强制要求的格式。

### 浏览器客户端

```javascript
const source = new EventSource('/events');

source.onopen = () => console.log('connected');
source.addEventListener('tick', event => {
	const data = JSON.parse(event.data);
	console.log(data.number, data.sentAt);
});
source.onerror = () => console.log('connection error');

// 不再需要接收事件时主动关闭连接
source.close();
```

浏览器的 `EventSource` 默认会在连接异常时自动重连。服务端可以通过 `retry` 字段建议重连间隔；如果服务端发送了 `id`，浏览器重连时通常会通过 `Last-Event-ID` 请求头告知服务端最后收到的事件编号。服务端需要自行保存事件或游标，才能根据这个编号补发遗漏事件。

### SSE 与其他方案

| 技术 | 通信方向 | 典型特点 | 适用场景 |
| --- | --- | --- | --- |
| SSE | 服务端到客户端 | 基于 HTTP，浏览器自动重连，文本事件流 | 通知、进度、实时日志、监控 |
| WebSocket | 双向 | 长连接，客户端和服务端都可以主动发送消息 | 聊天、协同编辑、实时游戏 |
| 轮询 | 客户端定时请求 | 实现简单，但会产生重复请求和延迟 | 更新频率低、基础设施受限的场景 |
| SignalR | 双向 | .NET 抽象层，可自动选择传输方式 | ASP.NET Core 中的实时业务功能 |

### 服务端实现注意事项

- 设置 `Content-Type: text/event-stream`，并关闭不必要的缓存。
- 每条事件末尾必须有空行，否则客户端可能一直等待事件结束。
- 及时刷新响应流，避免事件滞留在服务器或代理的缓冲区中。
- 使用请求的取消令牌，在客户端断开时停止后台任务，避免连接泄漏。
- 经过反向代理时，需要检查代理的响应缓冲、超时和连接数限制。
- 可以定期发送注释心跳，例如 `: keep-alive\n\n`，防止中间设备关闭空闲连接。
- 不要通过 SSE 传输密码、令牌等敏感信息；SSE 连接应使用 HTTPS，并遵循正常的认证和授权策略。

SSE 适合通知、进度更新、实时日志和监控数据等场景。如果应用需要客户端和服务端双向实时通信，可以考虑使用 WebSocket 或 SignalR。
