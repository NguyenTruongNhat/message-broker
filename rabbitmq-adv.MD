Đúng. Nếu chưa nắm các khái niệm này thì các câu hỏi RabbitMQ production sẽ rất khó, vì interviewer thường không hỏi riêng từng config mà ghép chúng vào một incident.

Bạn có thể nhìn RabbitMQ production theo flow này:

```text
Producer
   |
   | publish
   v
Exchange
   |
   | routing
   v
Queue
   |
   | deliver
   v
Consumer
   |
   +---- success ---> ACK
   |
   +---- fail ------> NACK
                        |
              +---------+---------+
              |                   |
          requeue=true        requeue=false
              |                   |
          Queue lại            DLX
                                  |
                                  v
                              Retry/DLQ
```

Mình giải thích từng phần từ cơ bản đến production.

---

# 1. ACK — Consumer nói với RabbitMQ: "Tôi xử lý xong rồi"

Đây là một trong những khái niệm quan trọng nhất.

Giả sử queue có:

```text
OrderCreated
```

Consumer nhận message:

```text
RabbitMQ
   |
   | OrderCreated
   v
Payment Service

1. Insert payment
2. Call payment provider
3. Update database
4. ACK
```

`ACK` nghĩa là:

> Consumer xác nhận với RabbitMQ rằng message này đã được xử lý xong.

Sau khi RabbitMQ nhận ACK, broker có thể loại message khỏi queue. RabbitMQ khuyến nghị application chỉ ACK sau khi công việc cần thiết đã hoàn thành. :chatgpt-content-reference{index="0"}

Ví dụ:

```text
Receive message
      |
      v
Process
      |
      v
Update DB
      |
      v
ACK
```

### Vì sao không ACK ngay khi nhận?

Ví dụ:

```text
Consumer nhận message

ACK
 |
 v
RabbitMQ delete message

Consumer xử lý...
        |
        X
     app crash
```

Message đã mất.

Thay vào đó:

```text
Consumer nhận
     |
Process
     |
Update DB
     |
ACK
```

Nếu consumer crash trước ACK, khi connection/channel đóng, RabbitMQ có thể đưa delivery chưa ACK trở lại queue để được xử lý lại. :chatgpt-content-reference{index="1"}

---

# 2. Auto ACK vs Manual ACK

Có hai tư duy chính.

### Auto ACK

RabbitMQ coi message như đã được consumer tiếp nhận mà không chờ application xác nhận xử lý business thành công.

Conceptually:

```text
RabbitMQ
   |
   | message
   v
Consumer
   |
RabbitMQ: "coi như xong"
```

Ưu điểm:

```text
fast
simple
```

Nhược điểm quan trọng:

```text
Consumer nhận
RabbitMQ coi message xong
        |
        X app crash
```

Có khả năng mất processing.

---

### Manual ACK

Production business processing thường sử dụng:

```text
autoAck = false
```

và consumer tự quyết định:

```text
success -> ACK
failure -> NACK / Reject
```

RabbitMQ cũng khuyến nghị manual acknowledgements cho các workload cần data safety; acknowledgements cùng với redelivery dẫn tới mô hình delivery thường được thiết kế theo `at-least-once`. :chatgpt-content-reference{index="2"}

---

# 3. NACK là gì?

`NACK = Negative Acknowledgement`.

Consumer nói:

> "Tôi nhận được message, nhưng xử lý thất bại."

Ví dụ:

```text
try
{
    Process(message);

    ACK();
}
catch
{
    NACK();
}
```

Nhưng NACK còn một option cực kỳ quan trọng:

```text
requeue = true
```

hoặc:

```text
requeue = false
```

---

# 4. NACK + requeue=true

Ví dụ:

```text
Queue
  |
  | Message A
  v
Consumer
  |
  X Database timeout

NACK(requeue=true)
  |
  v
Queue
```

RabbitMQ đặt message trở lại để có thể deliver lại.

Nghe rất hợp lý.

Nhưng có một vấn đề production kinh điển.

---

## Poison message

Giả sử message:

```json
{
  "orderId": null
}
```

Consumer:

```text
Message
   |
   v
Validate
   |
   X invalid

NACK requeue=true
```

Sau đó:

```text
Queue
 ↓
Consumer
 ↓
Fail
 ↓
Queue
 ↓
Consumer
 ↓
Fail
 ↓
Queue
 ...
```

=> **infinite retry loop**.

CPU tăng, logs tăng, queue bị churn và consumer có thể dành phần lớn tài nguyên cho một message không bao giờ xử lý được.

Đó là lý do production không nên đơn giản:

```text
catch(Exception)
{
    Nack(requeue:true);
}
```

cho mọi lỗi.

---

# 5. NACK + requeue=false

Khi:

```text
NACK(requeue=false)
```

RabbitMQ không đưa message trở lại queue ban đầu.

Nếu queue đã cấu hình Dead Letter Exchange:

```text
Main Queue
    |
 NACK(false)
    |
    v
   DLX
    |
    v
Dead Letter Queue
```

RabbitMQ có thể dead-letter message khi consumer `reject/nack` với `requeue=false`, khi message hết TTL, queue vượt giới hạn, hoặc trong một số trường hợp quorum queue vượt delivery limit. :chatgpt-content-reference{index="3"}

---

# 6. DLX và DLQ — hai khái niệm rất hay bị nhầm

Đây là điểm mình muốn bạn nhớ kỹ.

### DLX

```text
Dead Letter Exchange
```

Nó là **exchange**.

### DLQ

```text
Dead Letter Queue
```

Nó là **queue** mà team thường tạo để chứa failed messages.

Ví dụ:

```text
                 Normal flow

Order Exchange
      |
      v
 order.queue
      |
      v
 Consumer


                  Error flow

 order.queue
      |
 NACK(false)
      |
      v
 order.dlx
      |
      v
 order.dead.queue
```

Trong RabbitMQ, DLX vẫn là một exchange bình thường; queue nguồn được cấu hình để dead-letter message đến exchange đó. RabbitMQ khuyến nghị dùng policies cho DLX khi có thể vì policy có thể thay đổi runtime, trong khi hard-code `x-dead-letter-exchange` vào queue arguments khó thay đổi hơn. :chatgpt-content-reference{index="4"}

---

# 7. Tại sao cần DLQ?

Ví dụ bạn có:

```text
100,000 messages
```

trong đó:

```text
99,997 success
3 malformed messages
```

Bạn không muốn 3 message lỗi làm hệ thống retry mãi.

Cho chúng vào:

```text
order.dead.queue
```

Sau đó đội vận hành có thể inspect:

```text
Message ID
Order ID
Reason
Exception
Retry count
Timestamp
```

Fix lỗi rồi replay nếu phù hợp.

---

# 8. Retry Queue

DLQ thường là nơi cuối cùng.

Nhưng nhiều lỗi chỉ là tạm thời.

Ví dụ:

```text
Payment API timeout
SQL deadlock
HTTP 503
Network timeout
```

Bạn muốn:

```text
retry later
```

chứ không phải:

```text
retry immediately
```

Một topology thường thấy:

```text
main.queue
    |
    | fail
    v
retry.exchange
    |
    v
retry.queue
    |
    | wait 30s
    v
main.exchange
    |
    v
main.queue
```

Đây là nơi **TTL + DLX** thường được kết hợp.

---

# 9. TTL — Time To Live

TTL nghĩa là message hoặc queue có thời gian sống giới hạn.

Ví dụ:

```text
TTL = 30 seconds
```

Message:

```text
retry.queue

Message A
   |
 wait 30 sec
   |
 expire
```

Khi message expire và queue có DLX:

```text
retry.queue
   |
 TTL expires
   |
   v
DLX
   |
   v
main.queue
```

RabbitMQ hỗ trợ message TTL ở mức queue hoặc per-message; TTL được biểu diễn theo milliseconds. :chatgpt-content-reference{index="5"}

Ví dụ flow retry:

```text
                 fail
                  |
                  v
Main Queue ---> Retry Queue
                  |
               TTL 30s
                  |
                  v
             Main Queue
```

---

# 10. Exponential Backoff Retry

Production tốt hơn thường không làm:

```text
1 sec
1 sec
1 sec
1 sec
```

mà có thể:

```text
Retry #1 -> 10 sec

Retry #2 -> 30 sec

Retry #3 -> 2 min

Retry #4 -> DLQ
```

Flow:

```text
main.queue
    |
  fail
    |
    v
retry.10s.queue
    |
    v
main.queue
    |
  fail
    |
    v
retry.30s.queue
    |
    v
main.queue
    |
  fail
    |
    v
retry.2m.queue
    |
    v
main.queue
    |
  fail
    |
    v
DLQ
```

Lý do rất thực tế.

Nếu SQL down 5 phút và bạn có:

```text
10,000 messages
```

mà retry ngay lập tức:

```text
10,000 request
10,000 fail
10,000 retry
10,000 fail
...
```

Bạn đang tự DDoS dependency của mình.

---

# 11. Prefetch — concept cực kỳ quan trọng

RabbitMQ thường push message đến consumer.

Giả sử:

```text
Queue

M1
M2
M3
M4
M5
...
M1000
```

Consumer có:

```text
prefetch = 10
```

RabbitMQ có thể gửi tối đa khoảng:

```text
10 unacknowledged messages

M1
M2
...
M10
```

cho consumer đó trước khi cần thêm ACK để mở chỗ cho delivery tiếp theo.

RabbitMQ mô tả prefetch như giới hạn số message đã được delivery nhưng chưa ACK; khi đạt giới hạn, broker ngừng gửi thêm cho đến khi có acknowledgement. `prefetch=0` nghĩa là không giới hạn. :chatgpt-content-reference{index="6"}

---

# 12. Prefetch giải quyết vấn đề gì?

Giả sử:

```text
Consumer RAM = 512 MB

Message = 5 MB
```

Nếu RabbitMQ push:

```text
1000 messages
```

Consumer có thể giữ lượng dữ liệu cực lớn trong memory.

Với:

```text
prefetch = 10
```

consumer chỉ có tối đa khoảng 10 delivery chưa ACK:

```text
RabbitMQ

M1  -> Consumer
M2  -> Consumer
...
M10 -> Consumer

STOP
```

Consumer ACK M1:

```text
ACK M1

RabbitMQ:
M11 -> Consumer
```

---

# 13. Prefetch = 1

Ví dụ:

```text
prefetch = 1
```

Flow:

```text
RabbitMQ
   |
   M1
   |
Consumer

processing...

ACK

RabbitMQ
   |
   M2
```

Ưu điểm:

```text
fair distribution
low consumer memory
```

Nhược điểm:

```text
lower throughput
```

RabbitMQ docs cũng lưu ý prefetch 1 là lựa chọn rất bảo thủ và có thể giảm throughput đáng kể; giá trị tối ưu phụ thuộc workload. :chatgpt-content-reference{index="7"}

Vì vậy Senior không nên nói:

> "Prefetch luôn nên bằng 1."

Mà nên nói:

> "Tôi chọn prefetch dựa trên processing time, concurrency, message size và benchmark."

---

# 14. Ready vs Unacked messages

RabbitMQ Management UI thường cho bạn thấy hai trạng thái rất quan trọng:

```text
Ready

Unacked
```

### Ready

Message đang chờ consumer.

```text
Queue

M1 READY
M2 READY
M3 READY
```

### Unacked

Message đã được gửi cho consumer nhưng consumer chưa ACK.

```text
RabbitMQ

M1 --------> Consumer
                |
             processing

M1 = Unacked
```

RabbitMQ phân biệt messages `ready for delivery` với messages `delivered but not yet acknowledged`. :chatgpt-content-reference{index="8"}

---

# 15. Debug qua Ready / Unacked

Ví dụ production:

```text
Ready = 500,000
Unacked = 10
```

Có khả năng:

```text
consumer quá chậm
consumer ít
prefetch thấp
downstream bottleneck
```

Ngược lại:

```text
Ready = 0
Unacked = 100,000
```

đáng nghi hơn:

```text
prefetch quá lớn
consumer nhận quá nhiều message
consumer processing bị stuck
consumer không ACK
```

Đây là cách Senior đọc RabbitMQ dashboard chứ không chỉ nhìn:

```text
Queue has many messages.
```

---

# 16. Durable Queue

Giả sử RabbitMQ restart.

Bạn muốn queue:

```text
order.queue
```

vẫn tồn tại.

Queue cần được khai báo durable.

Conceptually:

```text
durable = true
```

```text
RabbitMQ restart

Before:
order.queue

After:
order.queue still exists
```

RabbitMQ khuyến nghị durable queues cho hầu hết workload lâu dài; quorum queues bắt buộc durable. :chatgpt-content-reference{index="9"}

---

# 17. Persistent Message

Đây là chỗ nhiều người nhầm.

Có hai vấn đề khác nhau:

```text
Queue survives restart
```

và:

```text
Message survives restart
```

Classic queue scenario:

```text
Durable Queue
+
Persistent Message
```

mới là cấu hình nền tảng cho việc giữ message qua broker restart. RabbitMQ docs phân biệt durable queue với persistent/transient messages; durable queue có thể được recover, còn transient message không có cùng semantics recovery. :chatgpt-content-reference{index="10"}

Nhưng vẫn còn một vấn đề.

Producer gọi:

```text
Publish(message)
```

không đồng nghĩa với:

> "Tôi biết chắc RabbitMQ đã safely nhận message."

Đó là lý do có Publisher Confirms.

---

# 18. Publisher Confirms

Đây là ACK nhưng ở phía **Producer**.

Đừng nhầm với Consumer ACK.

Hai chiều:

```text
Producer                       Consumer
   |                              |
   | publish                      |
   v                              |
RabbitMQ ------------------------>|
   |                              |
   | publisher confirm            |
   v                              |
Producer                          |
                                  |
                               process
                                  |
                                  | ACK
                                  v
                              RabbitMQ
```

RabbitMQ docs nhấn mạnh rằng việc producer ghi bytes xuống socket không đủ để biết broker đã nhận và xử lý publish; publisher confirms được dùng để xác nhận broker đã nhận trách nhiệm đối với message. :chatgpt-content-reference{index="11"}

---

# 19. Consumer ACK và Publisher Confirm hoàn toàn khác nhau

Bạn nên nhớ bảng này:

| Mechanism | Ai gửi? | Gửi cho ai? | Ý nghĩa |
|---|---|---|---|
| Publisher Confirm | RabbitMQ | Producer | Broker đã nhận trách nhiệm message |
| Consumer ACK | Consumer | RabbitMQ | Consumer xử lý xong |
| NACK | Consumer | RabbitMQ | Consumer xử lý thất bại |

Production pipeline:

```text
Producer
   |
 publish
   |
   v
RabbitMQ
   |
 confirm
   v
Producer


RabbitMQ
   |
 deliver
   v
Consumer
   |
 process
   |
 ACK
   v
RabbitMQ
```

Đây là **hai acknowledgement độc lập**. :chatgpt-content-reference{index="12"}

---

# 20. Exchange

Một misconception phổ biến:

```text
Producer -> Queue
```

Thực tế AMQP routing thường là:

```text
Producer
   |
   v
Exchange
   |
   v
Queue
```

Exchange có nhiệm vụ:

> route message đến queue phù hợp.

RabbitMQ định nghĩa exchange là nơi publisher publish message; exchange dùng type và bindings để route message tới một hoặc nhiều queue/stream/exchange khác. :chatgpt-content-reference{index="13"}

---

# 21. Direct Exchange

Ví dụ:

```text
Exchange: order.exchange
```

bindings:

```text
routing key = order.created
        |
        v
order-created.queue


routing key = order.cancelled
        |
        v
order-cancelled.queue
```

Producer:

```text
RoutingKey = "order.created"
```

Direct exchange match chính xác routing key với binding key. :chatgpt-content-reference{index="14"}

---

# 22. Fanout Exchange

Fanout bỏ qua routing key.

```text
             order.exchange
             FANOUT
          /      |       \
         /       |        \
        v        v         v
Audit Queue  Email Queue Analytics Queue
```

Một message được broadcast tới tất cả queue được bind.

RabbitMQ fanout exchange route một copy của message tới mọi destination đang bound và bỏ qua routing key. :chatgpt-content-reference{index="15"}

---

# 23. Topic Exchange

Đây là loại rất hữu ích cho event-driven systems.

Message:

```text
order.created.vn
order.created.us
order.cancelled.vn
```

Bindings:

```text
order.*
```

hoặc:

```text
order.created.*
```

hoặc:

```text
order.#
```

Trong topic exchange:

```text
* = đúng một segment
# = zero hoặc nhiều segment
```

Ví dụ RabbitMQ:

```text
audit.events.#
```

có thể match nhiều cấp phía sau. :chatgpt-content-reference{index="16"}

---

# 24. Connection và Channel

Đây cũng là câu senior rất hay gặp.

Connection là:

```text
Application
     |
     | TCP connection
     |
 RabbitMQ
```

TCP connection tương đối expensive.

AMQP tạo abstraction:

```text
Connection

     |
     +--- Channel 1
     |
     +--- Channel 2
     |
     +--- Channel 3
```

RabbitMQ mô tả channel giống như những **lightweight logical connections** multiplexed trên cùng một TCP connection. :chatgpt-content-reference{index="17"}

Có thể liên tưởng:

```text
Connection
≈ highway

Channel
≈ lanes
```

---

# 25. Vì sao không tạo connection cho mỗi message?

Bad:

```text
HTTP request

Create RabbitMQ connection
Publish
Close

HTTP request

Create RabbitMQ connection
Publish
Close
```

Nếu:

```text
1000 req/s
```

=> liên tục TCP connection/authentication/setup/teardown.

Thường production dùng:

```text
Application lifetime

RabbitMQ Connection
        |
        + Channel
        + Channel
        + Channel
```

Nhưng channel concurrency/thread-safety phải tuân theo RabbitMQ client bạn đang sử dụng; đừng đơn giản share một channel tùy tiện giữa mọi operation.

---

# 26. Classic Queue vs Quorum Queue

Đây là khái niệm khá mới nếu bạn học RabbitMQ từ tutorial cũ.

### Classic Queue

Queue truyền thống của RabbitMQ.

Ở RabbitMQ 4.x, classic queue là single-replica queue. :chatgpt-content-reference{index="18"}

Conceptually:

```text
Node A

order.queue
```

Node A chết có thể ảnh hưởng availability của queue đó.

---

### Quorum Queue

Quorum queue dùng replication dựa trên Raft.

Ví dụ:

```text
        Quorum Queue

Node A       Node B       Node C
Leader       Replica      Replica
   \           |           /
      replicated
```

RabbitMQ mô tả quorum queue là durable replicated queue dựa trên Raft, phù hợp khi data safety và HA quan trọng. Publisher confirm chỉ được phát khi message đã được replicate tới quorum cần thiết. :chatgpt-content-reference{index="19"}

Production critical business:

```text
Payments
Orders
Financial transactions
```

thường là nơi bạn sẽ cân nhắc quorum queues.

---

# 27. At-most-once và At-least-once

Đây không hẳn là config mà là delivery semantics.

## At-most-once

```text
message có thể:

0 lần
hoặc
1 lần
```

Không duplicate, nhưng có thể mất.

---

## At-least-once

```text
message:

1 lần
hoặc
2 lần
hoặc
3 lần...
```

Không muốn mất message, nhưng **phải chấp nhận khả năng duplicate**.

Manual acknowledgement + redelivery thường dẫn bạn tới thiết kế at-least-once. RabbitMQ cũng cảnh báo consumers phải chuẩn bị xử lý redelivery và nên thiết kế idempotent. :chatgpt-content-reference{index="20"}

---

# 28. Tại sao RabbitMQ có duplicate?

Scenario cực kỳ quan trọng:

```text
RabbitMQ
    |
    | Message A
    v
Consumer

Update DB SUCCESS

        ↓

Consumer chuẩn bị ACK

        ↓

APP CRASH
```

RabbitMQ không nhận ACK.

RabbitMQ nghĩ:

```text
Message chưa hoàn tất
```

=> redeliver.

Consumer nhận lại:

```text
Message A
```

=> duplicate processing.

Đây không phải bug.

Đây là hệ quả của at-least-once semantics. :chatgpt-content-reference{index="21"}

---

# 29. Idempotent Consumer

Để chống duplicate.

Message:

```json
{
    "messageId": "A123",
    "orderId": 1001
}
```

Consumer check:

```text
ProcessedMessages

MessageId
---------
A123
```

Flow:

```text
Receive A123
     |
Already processed?
     |
  +--+--+
 YES    NO
 |       |
ACK    Process
        |
       Save
        |
       ACK
```

Hoặc dựa vào unique business constraint:

```text
Payment

OrderId UNIQUE
```

Nếu Order 1001 đã payment:

```text
don't create again
```

Đây là lý do trong production bạn thường thấy:

```text
At-least-once
+
Idempotency
```

đi cùng nhau.

---

# 30. CorrelationId

CorrelationId không giúp RabbitMQ delivery.

Nó giúp **trace request/event xuyên services**.

Ví dụ:

```text
HTTP Request

CorrelationId:
abc-123

Order Service
     |
     | OrderCreated
     | correlationId=abc-123
     v
Payment Service
     |
     | PaymentCompleted
     | correlationId=abc-123
     v
Notification Service
```

Khi production lỗi:

```text
search logs:

abc-123
```

Bạn nhìn được toàn bộ journey.

---

# 31. MessageId và CorrelationId khác nhau

Rất dễ nhầm.

```text
MessageId
```

identify **message này**.

```text
CorrelationId
```

identify **business/request flow**.

Ví dụ:

```text
CorrelationId = ORDER-1001

MessageId = M001 -> OrderCreated
MessageId = M002 -> PaymentStarted
MessageId = M003 -> PaymentCompleted
MessageId = M004 -> EmailSent
```

Một correlation có thể gồm nhiều messages.

---

# 32. Outbox Pattern

Outbox không phải RabbitMQ configuration, nhưng gần như chắc chắn xuất hiện trong senior interview.

Problem:

```text
Order Service

BEGIN TRANSACTION

INSERT Order

COMMIT

Publish RabbitMQ
       |
       X RabbitMQ down
```

Database:

```text
Order = created
```

RabbitMQ:

```text
không có OrderCreated
```

Data inconsistent.

---

## Transactional Outbox

Thay vì:

```text
DB
then
RabbitMQ
```

ta làm:

```text
BEGIN TRANSACTION

INSERT Order

INSERT OutboxMessage

COMMIT
```

Database:

```text
Orders

Id
1001


OutboxMessages

Id      Event
M001    OrderCreated
```

Background worker:

```text
Outbox
   |
   v
RabbitMQ
   |
 Confirm
   |
   v
Mark published
```

Đây là cách giải quyết dual-write problem.

Nhưng Outbox vẫn có thể publish duplicate:

```text
Publish success
       |
       X crash
       |
not marked published
```

Worker restart:

```text
publish again
```

Vì thế:

```text
Outbox
+
Idempotent Consumer
```

thường đi cùng nhau.

---

# 33. Một RabbitMQ production topology hoàn chỉnh

Bây giờ ghép mọi khái niệm lại:

```text
                        Order Service
                             |
                             |
                    DB Transaction
                    Order + Outbox
                             |
                             v
                       Outbox Worker
                             |
                          Publish
                             |
                             v
                     order.exchange
                             |
                   Publisher Confirm
                             |
                             v
                       order.queue
                             |
                     prefetch = N
                             |
                             v
                     Payment Consumer
                        /         \
                    success       failure
                      |              |
                     ACK        retryable?
                                  /     \
                                YES      NO
                                 |        |
                              Retry      DLQ
                               Queue
                                 |
                                TTL
                                 |
                                 v
                            order.queue
```

Và consumer:

```text
              Message
                 |
                 v
           Check MessageId
                 |
          already processed?
              /       \
            yes        no
             |          |
            ACK      Process
                         |
                      DB TX
                         |
                        ACK
```

Đây chính là RabbitMQ production ở mức kiến trúc.

---

# 34. Những khái niệm bạn nên thuộc trước khi luyện interview

Mình khuyên học theo thứ tự này, vì chúng phụ thuộc nhau:

1. **Exchange → Queue → Binding → Routing Key**
2. **Consumer ACK / NACK**
3. **requeue**
4. **Ready / Unacked**
5. **Prefetch / QoS**
6. **DLX / DLQ**
7. **TTL**
8. **Retry + exponential backoff**
9. **Durable queue + message persistence**
10. **Publisher Confirms**
11. **At-most-once / At-least-once**
12. **Idempotency**
13. **Connection / Channel**
14. **Classic vs Quorum Queue**
15. **MessageId / CorrelationId**
16. **Transactional Outbox**

Nếu 16 khái niệm này bạn hiểu chắc, thì phần lớn câu hỏi **RabbitMQ Senior .NET production** sẽ không còn giống các kiến thức rời rạc nữa. Bạn sẽ nhìn một câu kiểu:

> **“Consumer update DB thành công nhưng crash trước ACK.”**

và lập tức suy ra chuỗi:

```text
No ACK
→ Redelivery
→ Duplicate
→ At-least-once
→ Idempotency
→ MessageId / unique constraint
```

Đó chính là kiểu tư duy interviewer muốn thấy ở Senior.
