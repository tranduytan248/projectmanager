---
name: unit-testing-test-generate
description: Tự động phân tích mã nguồn và sinh bộ Unit Test hoàn chỉnh, chất lượng cao cho cả Dart (Flutter BrewTask) và C# (.NET Framework MVC 5). Tự động nhận diện các kịch bản kiểm thử: Happy Path, Edge Cases (null, empty, boundary), Exception Handling, sinh Mock objects chuẩn xác và assertion nghiêm ngặt.
---

# ⚡ Skill: Unit Testing Test Generate — Tự Động Sinh Unit Test Toàn Diện

## 🎯 Mục đích

Skill này chuyên sâu về việc **phân tích mã nguồn và tự động tạo ra các bộ Unit Test tiêu chuẩn cao**. Không chỉ tạo test qua loa, skill đảm bảo sinh ra các ca kiểm thử chặt chẽ, bao phủ các trường hợp biên hiểm hóc, giả lập (mock) dependencies sạch sẽ và tuân thủ các quy chuẩn lập trình của dự án.

---

## 📐 Cấu Trúc Kiểm Thử Chuẩn: Arrange - Act - Assert (AAA)

Mọi unit test được sinh ra bắt buộc tuân theo khuôn mẫu 3 bước rõ ràng:
1. **Arrange (Chuẩn bị)**: Khởi tạo đối tượng cần test, thiết lập dữ liệu giả lập (fixtures) và định nghĩa hành vi của các mock dependencies.
2. **Act (Hành động)**: Thực thi hàm/phương thức cần kiểm thử với tham số đầu vào cụ thể.
3. **Assert (Xác minh)**: Kiểm tra giá trị trả về, trạng thái đối tượng, hoặc xác minh số lần gọi hàm của mock (verify).

---

## 🎯 Chiến Lược Bao Phủ 3 Tầng Kịch Bản

Khi phân tích bất kỳ hàm hoặc phương thức nào, luôn tự động sinh tối thiểu 3 nhóm test cases:

| Tầng Kịch Bản | Mục Tiêu Kiểm Thử | Ví Dụ Đầu Vào |
|---|---|---|
| **1. Happy Path** | Dữ liệu hợp lệ, dòng xử lý chuẩn mực nhất. | Input chuẩn, ID hợp lệ, danh sách có 3-5 phần tử. |
| **2. Edge Cases** | Các giá trị biên dễ gây lỗi logic hoặc crash. | Tham số null, chuỗi rỗng `""`, mảng rỗng `[]`, số 0, số âm, số vượt giới hạn int32, ngày quá khứ/tương lai xa. |
| **3. Error Handling** | Bắt lỗi đúng khi có sự cố hoặc vi phạm nghiệp vụ. | Quăng ngoại lệ `ArgumentNullException`, `ValidationException`, HTTP 401 Unauthorized, mất kết nối mạng. |

---

## 💻 Mẫu Sinh Test Cho Flutter / Dart

### 1. Phân Tích & Sinh Test Cho Service / Repository Logic

```dart
// test/features/task_management/services/task_service_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';

// Import các model và service của dự án
import 'package:brew_task/features/task_management/models/task_model.dart';
import 'package:brew_task/features/task_management/services/task_service.dart';
import 'package:brew_task/core/network/api_client.dart';

// Sinh mock class
import 'task_service_test.mocks.dart';

@GenerateMocks([ApiClient])
void main() {
  late TaskService taskService;
  late MockApiClient mockApiClient;

  setUp(() {
    mockApiClient = MockApiClient();
    taskService = TaskService(apiClient: mockApiClient);
  });

  group('TaskService - getTasksByProject', () {
    const projectId = 101;

    test('Happy Path: Trả về danh sách công việc khi API thành công', () async {
      // Arrange
      final fakeJson = [
        {'id': 1, 'title': 'Lập kế hoạch sprint', 'status': 'InProgress'},
        {'id': 2, 'title': 'Thiết kế màn hình Dashboard', 'status': 'Done'},
      ];
      when(mockApiClient.get('/api/projects/$projectId/tasks'))
          .thenAnswer((_) async => {'success': true, 'data': fakeJson});

      // Act
      final result = await taskService.getTasksByProject(projectId);

      // Assert
      expect(result, isNotNull);
      expect(result.length, equals(2));
      expect(result.first.title, equals('Lập kế hoạch sprint'));
      verify(mockApiClient.get('/api/projects/$projectId/tasks')).called(1);
    });

    test('Edge Case: Trả về danh sách rỗng khi API trả mảng trống', () async {
      // Arrange
      when(mockApiClient.get('/api/projects/$projectId/tasks'))
          .thenAnswer((_) async => {'success': true, 'data': []});

      // Act
      final result = await taskService.getTasksByProject(projectId);

      // Assert
      expect(result, isEmpty);
    });

    test('Error Handling: Ném Exception cụ thể khi API trả lỗi 500', () async {
      // Arrange
      when(mockApiClient.get('/api/projects/$projectId/tasks'))
          .thenThrow(ApiException(statusCode: 500, message: 'Lỗi máy chủ nội bộ'));

      // Act & Assert
      expect(
        () async => await taskService.getTasksByProject(projectId),
        throwsA(isA<ApiException>()),
      );
    });
  });
}
```

---

## 💻 Mẫu Sinh Test Cho Backend C# (.NET Framework 4.8 / ASP.NET MVC 5)

```csharp
// ProjectManager.Tests/Services/TaskServiceTests.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using ProjectManager.Models;
using ProjectManager.Repositories;
using ProjectManager.Services;

namespace ProjectManager.Tests.Services
{
    [TestFixture]
    public class TaskServiceTests
    {
        private Mock<ITaskRepository> _mockRepo;
        private TaskService _service;

        [SetUp]
        public void Setup()
        {
            _mockRepo = new Mock<ITaskRepository>();
            _service = new TaskService(_mockRepo.Object);
        }

        [Test]
        public async Task AssignTaskAsync_HappyPath_UpdatesStatusAndAssignee()
        {
            // Arrange
            int taskId = 42;
            int userId = 10;
            var existingTask = new ProjectTask { Id = taskId, Title = "Viết API chấm KPI", Status = "New" };

            _mockRepo.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(existingTask);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<ProjectTask>())).ReturnsAsync(true);

            // Act
            var result = await _service.AssignTaskAsync(taskId, userId);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(userId, existingTask.AssignedUserId);
            Assert.AreEqual("Assigned", existingTask.Status);
            _mockRepo.Verify(r => r.UpdateAsync(existingTask), Times.Once);
        }

        [Test]
        public void AssignTaskAsync_TaskNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            int invalidTaskId = 999;
            _mockRepo.Setup(r => r.GetByIdAsync(invalidTaskId)).ReturnsAsync((ProjectTask)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _service.AssignTaskAsync(invalidTaskId, 1);
            });
            Assert.That(ex.Message, Does.Contain("không tồn tại"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void AssignTaskAsync_InvalidUserId_ThrowsArgumentException(int invalidUserId)
        {
            // Arrange & Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.AssignTaskAsync(10, invalidUserId);
            });
        }
    }
}
```

---

## 📋 Checklist Xác Nhận Chất Lượng Unit Test Được Sinh Ra

- [ ] Bài test độc lập hoàn toàn, không phụ thuộc vào thứ tự chạy?
- [ ] Tên hàm test rõ ràng: `MethodName_Condition_ExpectedBehavior`?
- [ ] Không có assertion yếu kiểu `expect(res != null)` đơn thuần, mà phải kiểm tra cụ thể nội dung thuộc tính?
- [ ] Đã mock toàn bộ I/O bên ngoài (Database connection, Network client, File system)?
- [ ] Đã kiểm tra đầy đủ các giá trị biên (0, null, chuỗi rỗng, số âm)?
- [ ] Đã kiểm tra ngoại lệ có kèm nội dung thông báo lỗi tiếng Việt dễ hiểu?
