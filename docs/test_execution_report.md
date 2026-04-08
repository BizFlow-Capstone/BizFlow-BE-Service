# Báo Cáo Thực Thi Kiểm Thử (Test Execution Report)

Tài liệu này ghi nhận chi tiết các kịch bản kiểm thử (Test Cases) đã được thực thi trên hệ thống BizFlow Backend. Việc kiểm thử được chia thành các mức độ: Unit Test (tại tầng Application Services) và API Test (tại tầng Controller).

---

## 1. Unit Test - Lập trình tại Application Layer

Danh sách chi tiết các test cases đã lập trình thông qua xUnit và Moq, tập trung vào Logic nghiệp vụ tại các Service.

### 1.1 BusinessLocation Service
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| `GetLocationDetailAsync_WhenNoAccess_ShouldThrowForbidden` | Chặn truy cập chi tiết chi nhánh nếu không có quyền | `locationId` không được phân quyền | `Throw ForbiddenException` | Passed |
| `GetLocationDetailAsync_WhenDetailMissing_ShouldThrowNotFound` | Xử lý khi chi nhánh tồn tại trên DB nhưng ko có detail dto | `locationId` hợp lệ nhưng thiếu dữ liệu join | `Throw NotFoundException` | Passed |
| `CreateLocationAsync_WithValidRequest_ShouldCreateAndReturnDto` | Đảm bảo tính năng tạo chi nhánh mới chạy đúng | `CreateLocationRequest` hợp lệ | Trả về `BusinessLocationDto` và insert assignment | Passed |
| `CreateLocationAsync_WhenNameDuplicated_ShouldThrowConflict` | Ngăn chặn tạo chi nhánh trùng tên của cùng 1 Owner | `CreateLocationRequest` có `Name` trùng lặp | `Throw ConflictException` | Passed |
| `AddEmployeesToLocationAsync_WhenEmployeeNotHired_ShouldThrowBadRequest` | Chặn phân công user chưa được thuê vào chi nhánh | `employeeId` không hợp lệ/chưa hired | `Throw BadRequestException` | Passed |
| `AddEmployeesToLocationAsync_WhenEmployeeAlreadyAssigned...` | Chặn phân công lặp lại nhân viên đã đóng vai trò ở Cửa hàng | `employeeId` đã assigned | `Throw BadRequestException` | Passed |
| `DeleteLocationAsync_WhenHasRelatedData_ShouldSoftDelete` | Chỉ xóa mềm (Soft-Delete) khi chi nhánh đã có phát sinh | `locationId` có phát sinh data | Set `DeletedAt`, không xóa cứng | Passed |
| `DeleteLocationAsync_WhenNoRelatedData_ShouldHardDelete` | Ưu tiên xóa cứng khi chi nhánh là giả hoặc vừa tạo nhầm | `locationId` rỗng, ko có data | Gọi `Repository.Delete()` | Passed |
| `ValidateLocationAccessAsync_WhenEmployeeAccessInactiveLocation...` | Khóa nhân viên truy cập chi nhánh bị "Inactive" | `locationId` có trạng thái Inactive | `Throw ForbiddenException` | Passed |
| `RemoveEmployeeFromLocationAsync_ShouldCallRepositoryAndSave` | Cho phép Owner gỡ nhân viên ra khỏi chi nhánh | `locationId`, `employeeId` hợp lệ | Cập nhật DB Assignment | Passed |

### 1.2 Formula Engine (Kế toán & Công thức)
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| `Evaluate_Literal_ShouldReturnValue` | Định giá trị hằng số (Literal) | Khối literal `{"literal": 42.5}` | Trả về `42.5` | Passed |
| `Evaluate_Ref_ShouldResolveFromPreviousFormula` | Tham chiếu biến đệ quy (Ref) | Phụ thuộc lẫn nhau giữa A và B | Tính thành công không lỗi circular | Passed |
| `Evaluate_Op_ShouldComputeCorrectly` | Thực thi các toán tử +, -, *, / chuẩn | Data test 4 cases (InlineData) | Ra đúng số kỳ vọng | Passed |
| `Evaluate_DivideByZero_ShouldReturnZero` | Xử lý an toàn khi chia cho số không (0) | `100 / 0` từ JSON | Trả về `0` (không gây Crash) | Passed |
| `Evaluate_Max_ShouldReturnLargest` | Lấy giá trị MAX an toàn | Tập các số `[5, 12, 8]` | Trả về `12` | Passed |
| `Evaluate_Min_ShouldReturnSmallest` | Lấy giá trị MIN an toàn | Tập các số `[5, 12, 3]` | Trả về `3` | Passed |
| `Evaluate_Abs_ShouldReturnAbsoluteValue` | Tính giá trị tuyệt đối (ABS) | `{"literal": -42}` | Trả về `42` | Passed |
| `Evaluate_RoundHalfUp_ShouldRoundCorrectly` | Làm tròn `RoundHalfUp` | Logic `10 / 3`, độ chính xác 2 | Trả về `3.33` | Passed |
| `Evaluate_Floor_ShouldRoundDown` | Làm tròn `Floor` | Làm tròn floor số `3.99` | Trả về `3` | Passed |
| `Evaluate_ComplexChain_ShouldResolveInOrder` | Kết hợp công thức phức tạp nhiều bước | Tăng dần các toán tử kết hợp | Tính theo quy trình luồng Order | Passed |
| `Evaluate_InvalidJson_ShouldDefaultToZero` | An toàn với JSON string gãy | Công thức lỗi cú pháp JSON | Trả về giá trị Fallback là `0` | Passed |
| `Evaluate_UnknownNodeShape_ShouldReturnZero` | An toàn với Node Json rác | AST schema rác `{"foo": "bar"}` | Trả về `0` | Passed |
| `Evaluate_DuplicateCode_ShouldKeepLastComputedValue` | Xử lý khai báo trùng lặp code | 2 object công thức trùng `Code` | Lấy giá trị Last Value | Passed |
| `CalculateTax_Group1_WithRevenueUnder500M_ShouldReturnZero` | Nhóm 1: Doanh thu <= 500M thì miễn thuế | JSON `threshold` <= 500M | Trả về `0` | Passed |
| `CalculateTax_Group2_Option1_ShouldSubtract500M_BeforeTax` | Nhóm 2: Trừ khoán 500M trước khi nhân thuế | JSON `deduction` = 500M | (DT - 500M) * Tax Rate | Passed |

### 1.3 AccountingBook Service
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| `CreateBooksAsync_WhenPeriodFinalized_ShouldThrowBadRequest` | Chặn tạo sổ khi kỳ kế toán đã chốt | `periodId` đã bị khóa (finalized) | `Throw BadRequestException` | Passed |
| `CreateBooksAsync_WhenPeriodNotFound_ShouldThrowNotFound` | Xử lý kỳ kế toán không có thực | `periodId` = 999 | `Throw NotFoundException` | Passed |
| `CreateBooksAsync_WhenNoActiveRuleset_ShouldThrowBadRequest` | Kiểm tra Ruleset Thuế luôn tồn tại và bật | `ActiveRuleset` is Null | `Throw BadRequestException` | Passed |
| `CreateBooksAsync_WithInvalidTemplateCode_ShouldThrowBadRequest` | Kiểm tra template ID | `TemplateCode` không tồn tại | `Throw BadRequestException` | Passed |
| `CreateBooksAsync_WithWrongGroup_ShouldThrowBadRequest` | Xác thực Nhóm Hình thức/Kế toán | Group trong Template ko khớp | `Throw BadRequestException` | Passed |
| `ListBooksAsync_ShouldReturnMappedDtos` | Trả về list sổ kế toán chuẩn dto | BusinessTypes join Template | Map thành `[BookListItemDto]` | Passed |
| `GetBookSummaryAsync_WhenBookNotFound_ShouldThrowNotFound` | Chặn lấy báo cáo sổ không có thực | DB trả về NULL | `Throw NotFoundException` | Passed |
| `GetBookSummaryAsync_WhenWrongLocation_ShouldThrowForbidden` | Chặn lấy báo cáo không dành cho cửa hàng | `bookId` ở Location khác | `Throw ForbiddenException` | Passed |

### 1.4 Cost & Revenue Service (Sổ chi phí & Doanh thu)
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| `CreateManualAsync_ShouldCreateCost_AndRecordGL` | Tạo phiếu chi và tự động kéo sổ General Ledger (GL) | Dữ liệu hợp lệ | Trả ID DTO, Gọi RecordManualCost | Passed |
| `CreateManualAsync_WhenPaymentMethodInvalid_ShouldThrowBadRequest` | Chặn Method "Tiền x" rác | `wire_transfer_unknown` | `Throw BadRequestException` | Passed |
| `CreateManualAsync_WhenOwnerValidationFails_ShouldThrowForbidden` | Chặn chi tiền nếu không phải đúng người dùng Owner | `BusinessLocationId` khác | `Throw ForbiddenException` | Passed |
| `CreateManualAsync_WhenBusinessTypeMissing_ShouldThrowBadRequest` | Đảm bảo Doanh Thu phải có BusinessType | Doanh thu thiếu Loại Cửa Hàng | `Throw BadRequestException` | Passed |
| `UpdateManualAsync_WhenRevenueNotFound_ShouldThrowNotFound` | Bảo vệ tránh update bản ghi không tồn tại | `revenueId = 99` | `Throw NotFoundException` | Passed |
| `UpdateManualAsync_WhenTypeNotManual_ShouldThrowBadRequest` | Không cho phép sửa Doanh Thu hệ thống (Sale) bằng API Manual | Edit Record `RevenueType.Sale` | `Throw BadRequestException` | Passed |
| `UpdateManualAsync_WhenValid_ShouldUpdateAndReverseThenRecordGl` | Cập nhật tự động đảo xuất nhập ở GL (Reverse Record) | Thông tin cập nhật + trim string | Xóa GL cũ, ghi GL mới; trim mô tả | Passed |
| `ListAsync_WhenInvalidRevenueType_ShouldThrowBadRequest` | Chặn filter với RevenueType không hỗ trợ | `RevenueType = "invalid_type"` | `Throw BadRequestException` | Passed |
| `ListAsync_WhenValid_ShouldNormalizeFiltersAndReturnPaged` | Normalize filter trước khi query (lowercase, trim) | `" manual "`, `" CASH "` | Filter normalized, kết quả phân trang | Passed |
| `DeleteManualAsync_WhenNotFound_ShouldThrowNotFound` | Bảo vệ tránh xóa bản ghi không tồn tại | `revenueId = 1` (null) | `Throw NotFoundException` | Passed |
| `DeleteManualAsync_WhenNotManual_ShouldThrowBadRequest` | Không cho phép xóa doanh thu hệ thống (Sale) | Record `RevenueType.Sale` | `Throw BadRequestException` | Passed |
| `DeleteManualAsync_WhenValid_ShouldSoftDeleteAndReverseGl` | Xóa mềm + đảo GL khi delete manual revenue | `revenueId` hợp lệ | Set `DeletedAt`, gọi Reverse GL | Passed |
| `DeleteManualAsync_ShouldSoftDelete_AndReverseGL` (Cost) | Xóa phiếu chi thủ công bằng cờ Soft-Delete | Hủy Cost an toàn | Gạch bỏ, chạy Reverse Cost GL | Passed |
| `DeleteManualAsync_WhenImportCost_ShouldThrowBadRequest` | Chặn xóa dòng phí nhập kho qua API Manual | Xóa Ticket phí nhập `CostType.Import` | `Throw BadRequestException` | Passed |

### 1.5 Debtor Service (Công nợ)
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| `ListAsync_WhenNoOwnedLocation_ShouldReturnEmptyPage` | Cắt quyền search user không có location | User Employee/Guest Query | `EmptyPage` (Bảo mật data) | Passed |
| `ListAsync_WhenContainsUnauthorizedLocation_ShouldThrowForbidden` | Chặn ID cửa hàng bẩn xỏ ngang filter | `ListIds = [1, 999]` (có 999) | `Throw ForbiddenException` | Passed |
| `CreateAsync_WhenPhoneDuplicated_ShouldThrowConflict` | Chặn duplicate thông tin con nợ theo SDT ở trong Location | `Phone = 0909` trùng lap | `Throw ConflictException` | Passed |
| `DeleteAsync_WhenOutstandingBalanceAndNoForce_ShouldThrowBadRequest`| Khi xóa công nợ mà tài khoản còn báo dư/nợ thì chặn | `CurrentBalance > 0` | `Throw BadRequestException` | Passed |
| `DeleteAsync_WhenHasActivity_ShouldSoftDelete` | Khách phát sinh hóa đơn -> chỉ Soft Delete | User có lịch sử Activity | Set `DeletedAt` | Passed |
| `RecordPaymentAsync_WhenAmountZero_ShouldThrowBadRequest` | Giao dịch trả nợ 0đ sẽ chặn rác DB | `Amount = 0` | `Throw BadRequestException` | Passed |
| `RecordPaymentAsync_WhenMethodInvalid_ShouldThrowBadRequest` | Phát sinh trả nợ phương thức ảo | `PaymentMethod = "bad_method"` | `Throw BadRequestException` | Passed |
| `RecordPaymentAsync_WhenDebtorInactive_ShouldThrowBadRequest` | Chặn thanh toán khi tài khoản công nợ đã bị khóa | Debtor `IsActive = false`, Amount âm | `Throw BadRequestException` | Passed |
| `RecordPaymentAsync_WhenValid_ShouldPersistAndRecordGl` | Cập nhật `CurrentBalance` tự động và ghi Audit GL | Paid `-40`, Balance cũ `100` | Balance cập nhật thành `60`, gọi GL | Passed |
| `RecordPayment_WhenAmountExceedsDebt_ShouldAllowPositiveBalance_AndLogGL`| Xử lý tiền khách trả dư | Giao dịch nạp lố qua hạn nợ | `CurrentBalance` hiển thị Dương, ghi GL | Passed |

### 1.6 Import Service (Nhập kho)
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| `GetTemplateAsync_WhenNoActiveSchema_ShouldThrowNotFound` | Nếu System Admin chưa Enable Schema nhập kho | API xin template form JSON | `Throw NotFoundException` | Passed |
| `CreateImportAsync_WhenConfirmWithoutReceivedAt_ShouldThrowBadRequest` | Phiếu Confirm mà không có dấu mộc Received | Tạo phiếu DRAFT -> CONFIRM | Văng lỗi do thiếu ReceivedAt | Passed |
| `CreateImportAsync_WhenImportTypeInvalid_ShouldThrowBadRequest` | Chặn Type sai | Form nhập có giá trị Enum lỗi | Văng BadRequest | Passed |
| `ListImportsAsync_WhenValid_ShouldNormalizeAndReturnItems` | Mapping phân trang | Trim Input Query ` draft ` | Clean Filters và tìm DB | Passed |
| `DeleteImportAsync_WhenAlreadyCancelled_ShouldThrowBadRequest` | Chặn hủy phiếu vốn dĩ đã ngưng | Ticket `Canceled` đưa vào | `Throw BadRequestException` | Passed |
| `DeleteImportAsync_WhenDraft_ShouldHardDelete` | Nếu rác (Draft) thì Hard-Clear để dọn DB | `Status: Draft` | Gọi `Repository.Delete()` cứng | Passed |

### 1.7 Employee & Role Services (Nhân sự)
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| `GetAllRolesAsync_WhenHasData_ShouldMapAllFields` | Trả về Profile Role đầy đủ cho FE Menu Permission | Dữ liệu Role Database | DTO hợp lệ RoleId, Tên | Passed |
| `GetRoleByNameAsync_WhenFound_ShouldMap` | Truy xuất động cho Business Checking logic hệ thống | Query Name ="owner" | Cấp object `Role` tương ứng | Passed |
| `GetEmployeeSummariesAsync_ShouldMapStatusAndBasicFields` | Format list cho thẻ Hire List Card trên frontend | SQL Join Avatar + Email | Map đúng `[EmployeeSummaryDto]` | Passed |
| `ValidateEmployeesForAssignmentAsync_WhenMixedAndDuplicated_ShouldDistinctAndSplit` | An toàn hóa đầu vào Dto mảng (Split) ID nhân sự | List Array có phần tử ảo, lặp | Valid 1, Invalid 1 | Passed |
| `InviteEmployeeAsync_WhenOwnerInvitesSelf_ShouldThrowBadRequest` | Chặn Owner tự mời chính mình làm nhân viên | `ownerId == employeeId` | `Throw BadRequestException` | Passed |
| `InviteEmployeeAsync_WhenAlreadyPending_ShouldThrowConflict` | Chặn gửi lời mời nhiều lần nếu đang pending/accepted | Hire `Status` = pending | `Throw ConflictException` | Passed |
| `RemoveEmployeeAsync_WhenHasActiveAssignments_ShouldThrowBadRequest` | Chặn gỡ nhân viên nếu còn phân công ở chi nhánh | `HasActiveAssignments`=true | `Throw BadRequestException` | Passed |
| `AcceptInvitationAsync_WhenValid_ShouldSetAcceptedAndSave` | Chấp nhận lời mời nhân sự | Lời mời hợp lệ | `IsActive=true`, Save DB | Passed |
| `RejectInvitationAsync_WhenValid_ShouldSetRejectedAndSave` | Từ chối lời mời nhân sự | Lời mời hợp lệ | `Status=rejected`, Save DB | Passed |

### 1.8 Order AutoMapper Profile
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| `Map_OrderWithNullCreatedBy_MapsNullCreatedByProfileId` | Đảm bảo AutoMapper xử lý null `CreatedBy` không bị crash | `Order.CreatedBy = null` | `OrderDto.CreatedByProfileId = null` | Passed |

### 1.9 Inventory & References (Kho và Danh mục tham chiếu)
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| `CreateStockMovement_WhenQuantityPositive_ShouldCreateInMovement` | + Tồn kho sẽ tạo TX type `In` | SL Dương | TX `In`, + Balance, Memo | Passed |
| `CreateStockMovement_WhenQuantityNegative_ShouldCreateOutMovement` | - Tồn kho sẽ tạo TX type `Out` | SL Âm | TX `Out`, - Balance, Memo | Passed |
| `CreateStockMovement_WhenQuantityIsZero_ShouldThrowBadRequest` | Chặn Log Stock vô nghĩa do 0đ | SL 0 | `Throw BadRequestException` | Passed |
| `ReferenceLists_ShouldNotContainDuplicates_CaseInsensitive` | Kiểm thử toàn bộ Memory References không được trùng Enum | Toàn bộ 16 list | Valid hoàn toàn Unique CaseInsensitive | Passed |
| `ReferenceMethods_ShouldBeDeterministic` | Kết quả gọi nhiều lần phải nhất quán (no side-effects) | Gọi `GetPaymentMethods()` 2 lần | Kết quả hai lần như nhau | Passed |

### 1.10 Product Service
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế | Lỗi/Log Message |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `GetProductDetailAsync_WhenNotFound_ShouldThrowNotFound` | Truy xuất sản phẩm không tồn tại | `productId` = 99 | `Throw NotFoundException` | Passed | N/A |
| `GetProductDetailAsync_WhenNoAccess_ShouldThrowForbidden` | Chặn truy cập nếu user không có quyền | Location không thuộc user | `Throw ForbiddenException` | Passed | `COMMON_FORBIDDEN` |
| `UpdateProductStatusAsync_WhenInvalidStatus_ShouldThrowBadRequest` | Chặn update status rác | `status` = "invalid" | `Throw BadRequestException` | Passed | N/A |
| `UpdateProductStatusAsync_WhenValid_ShouldNormalizeAndSave` | Cập nhật status hợp lệ và parse string | `status` = "INACTIVE" | Cập nhật thành "inactive" | Passed | N/A |
| `DeleteProductAsync_WhenNoHistory_ShouldHardDelete` | Xóa cứng sản phẩm chưa bán/nhập | Không có `SaleItems` | Gọi `Repository.Delete()` | Passed | N/A |
| `DeleteProductAsync_WhenHasHistory_ShouldSoftDelete` | Xóa mềm khi sản phẩm đã phát sinh GD | Có `SaleItems` | Set `DeletedAt` | Passed | N/A |
| `BulkAdjustSellingPriceAsync_WhenEmptyList_ShouldThrowBadRequest` | Chặn chỉnh giá với list rỗng | `SaleItemIds` rỗng | `Throw BadRequestException` | Passed | N/A |
| `CreateProduct_WhenUserIsEmployee_ShouldThrowForbidden` | Phân quyền: Nhân viên không được phép tạo hàng hóa | IsOwner = false | `Throw ForbiddenException` | Passed | `COMMON_FORBIDDEN` |

### 1.11 Order Service
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế | Lỗi/Log Message |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `CompleteAsync_WhenOrderNotFound_ShouldThrowNotFound` | Chốt đơn ảo | `orderId` không tồn tại | `Throw NotFoundException` | Passed | N/A |
| `CompleteAsync_WhenAlreadyCompleted_ShouldThrowBadRequest` | Chặn hoàn thành đơn đã chốt | `Status = Completed` | `Throw BadRequestException` | Passed | N/A |
| `CancelAsync_WhenAlreadyCancelled_ShouldThrowBadRequest` | Chặn hủy đơn đã hủy | `Status = Cancelled` | `Throw BadRequestException` | Passed | N/A |
| `ListAsync_WhenInvalidStatus_ShouldThrowBadRequest` | Liệt kê đơn với status rác | `Status = invalid` | `Throw BadRequestException` | Passed | N/A |
| `UpdateAsync_WhenStatusNotPending_ShouldThrowBadRequest` | Chặn sửa đơn khi không còn Pending | `Status = Cancelled` | `Throw BadRequestException` | Passed | N/A |
| `CreateAsync_WithStalePriceFromFE_ShouldUseCurrentPolicyPrice` | Xử lý chênh lệch giá (FE dùng giá cũ) | Giá FE sai lệch giá Backend | Ignore giá FE, dùng Product Policy BE | Passed | N/A |
| `CompleteAsync_WithNegativeInventory_ShouldNotBlock_WhenTrackInventoryTrue` | Luồng bán hàng: Cho phép bán âm kho | Tồn kho `< 0` | Không Crash, Stock tụt xuống âm hơn | Passed | N/A |
| `CreateAsync_WhenDebtExceedsLimit_ShouldReturnWarning_InsteadOfBlock` | Hạn mức tín dụng: Quá Debit Limit chỉ Cảnh Báo | `Debt > CreditLimit` | Order có cờ `RequiresConfirmation=True`| Passed | N/A |
| `CompleteAsync_WhenCurrentBalanceIsPositive_ShouldDeductUsingDebtAmount` | Bù trừ thanh toán: Dùng số tiền dư của khách để cấn nợ | `Debtor.CurrentBalance > 0` | Khách tự động trừ số dư tiền dương | Passed | N/A |

### 1.12 Accounting Period Service (Kỳ kế toán)
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế | Lỗi/Log Message |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `CreatePeriodAsync_WhenInvalidPeriodType_ShouldThrowBadRequest` | Chặn loại kỳ rác | `PeriodType = invalid` | `Throw BadRequestException` | Passed | N/A |
| `CreatePeriodAsync_WhenYearly_WithQuarterProvided...` | Chặn kỳ nguyên NĂM nhưng gửi kèm QUÝ | `type=year`, `quarter=1`| `Throw BadRequestException` | Passed | N/A |
| `CreateCustomPeriodAsync_WhenEndDateBeforeStartDate...` | Chặn kỳ tùy chọn có ngày lệch | `EndDate < StartDate` | `Throw BadRequestException` | Passed | N/A |
| `FinalizePeriodAsync_WhenAlreadyFinalized_ShouldThrowBadRequest` | Chặn khóa sổ khi đã khóa | Kỳ đã `Finalized` | `Throw BadRequestException` | Passed | `PeriodAlreadyFinalized` |
| `FinalizePeriodAsync_WhenNoBooksExist_ShouldThrowBadRequest` | Khóa sổ khi chưa có sổ sách | K có `Books` | `Throw BadRequestException` | Passed | `PeriodNoBooks` |
| `ReopenPeriodAsync_WhenReasonEmpty_ShouldThrowBadRequest` | Bắt buộc có lý do mở lại kỳ | `reason = " "` | `Throw BadRequestException` | Passed | N/A |
| `DeletePeriodAsync_WhenHasBooks_ShouldThrowBadRequest` | Chặn xóa kỳ có dữ liệu | `CountActiveBooks > 0` | `Throw BadRequestException` | Passed | N/A |

### 1.13 General Ledger Service
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế | Lỗi/Log Message |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `ListAsync_WhenInvalidViewMode_ShouldThrowBadRequest` | Chặn view mode rác | `ViewMode = bad` | `Throw BadRequestException` | Passed | `LedgerInvalidViewMode` |
| `ListAsync_WhenInvalidTransactionType_ShouldThrowBadRequest` | Chặn query loai TX linh tinh | List TX có rác | `Throw BadRequestException` | Passed | `LedgerInvalidTransactionType` |
| `ListAsync_WhenFromDateAfterToDate_ShouldThrowBadRequest` | Chặn filter ngược ngày tháng | `FromDate > ToDate` | `Throw BadRequestException` | Passed | `LedgerInvalidDateRange` |
| `ListAsync_WhenValidQuery_ShouldReturnPagedResults` | Xử lý map sổ GL (đảo, audit mode) | Data chuẩn | Trả về page GL Items | Passed | N/A |

### 1.14 OTP Service
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế | Lỗi/Log Message |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `SendOtpAsync_WhenAccountNotFound_ShouldThrowBadRequest` | Chặn reset email gửi bừa bãi không tốn tại | Mailsảo | `Throw BadRequestException` | Passed | N/A |
| `SendOtpAsync_WhenRoleIsAdmin_ShouldThrowForbidden` | Chặn lộ lọt tài khoản Admin từ ngoài luồng | `role = admin` | `Throw ForbiddenException` | Passed | N/A |
| `SendOtpAsync_WhenAccountInactive_ShouldThrowUnauthorized` | Chặn tài khoản bị khóa lấy lại MK | `IsActive=false` | `Throw UnauthorizedException` | Passed | N/A |
| `SendOtpAsync_WhenRateLimited_ShouldThrowBadRequest` | Chống spam email lấy OTP liên tục | Request liên tục < 1p | `Throw BadRequestException` | Passed | N/A |
| `SendOtpAsync_WhenValid_ShouldCreateOtpAndSendEmail` | Sinh OTP mới, xóa OTP cũ, chuẩn | Hợp lệ | Lưu Database & SendTemplate | Passed | N/A |
| `VerifyEmailOtp_WhenInputInvalid_ShouldThrowBadRequest` | Chặn input rỗng | OTP rỗng | `Throw BadRequestException` | Passed | N/A |
| `VerifyEmailOtp_WhenInvalidOtp_ShouldThrowBadRequest` | Chặn mã OTP sai hoặc quá 5p | OTP sai lệch DB | `Throw BadRequestException` | Passed | N/A |
| `VerifyEmailOtp_WhenValid_ShouldSetNonceAndReturnResult` | Kiểm duyệt thành công và cấp vé Nonce | Hợp lệ | Map `PasswordResetNonce` để API sau dùng reset password | Passed | N/A |

### 1.15 Authentication & Identity Service
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế | Lỗi/Log Message |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `LoginWithEmailAsync_WhenNotFound_ShouldThrowUnauthorized` | Đăng nhập tài khoản không có thật | Sai email | `Throw UnauthorizedException` | Passed | N/A |
| `LoginWithEmailAsync_WhenWrongPassword_ShouldThrowUnauthorized` | Kiểm thử hash password mismatch | Sai MK | `Throw UnauthorizedException` | Passed | N/A |
| `LoginWithEmailAsync_WhenValid_ShouldReturnTokens` | Luồng đăng nhập tiêu chuẩn trả JWT Access & Refresh | Email & MK hợp lệ | Trả về thông tin Session (AccessToken, RefreshToken) | Passed | N/A |
| `SetPasswordAsync_WhenAlreadySet_ShouldThrowInvalidOperation` | Không cho phép ghi đè/tạo password khi đã có pass | TK đã có Hash cũ | `Throw InvalidOperationException` | Passed | N/A |
| `ChangePasswordAsync_WhenNoPasswordSet_ShouldThrowInvalidOperation` | Đổi pass trong khi trước giờ login Google không có pass | Google Account (null pass) | `Throw InvalidOperationException` | Passed | N/A |
| `ChangePasswordAsync_WhenValid_ShouldChangeAndRevokeTokens` | Đổi MK an toàn sẽ bắt văng TẤT CẢ device khác | MK cũ & MK mới đúng | Đăng xuất toàn hệ thống (RevokeAllTokens) và Update Hash | Passed | N/A |
| `UpdateProfileInfoAsync_WhenEmptyName_ShouldThrowArgumentException` | Validation chặn khoảng trắng | Name = `  ` | `Throw ArgumentException` | Passed | N/A |

### 1.16 Subscription Plan Service (Admin)
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế | Lỗi/Log Message |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `CreatePlanAsync_WhenFeatureNotFound_ShouldThrowBadRequest` | Chặn tạo plan trỏ đến feature ID rác | `FeatureId = 99` không tồn tại | `Throw BadRequestException` | Passed | N/A |
| `CreatePlanAsync_WhenDiscountRulesViolated_ShouldThrowBadRequest` | Validation quy tắc Discount Start/End ngược đời | `DiscountStart > DiscountEnd` | `Throw BadRequestException` | Passed | N/A |
| `CreatePlanAsync_WhenValid_ShouldSaveAndReturnDto` | Flow cơ bản lưu plan giá chuẩn bị bán | Valid prices, 30 days | `Return AdminSubscriptionPlanDto` | Passed | N/A |
| `SetPlanStatusAsync_WhenActivatingWithoutPrice_ShouldThrowBadRequest` | Chặn bật bán gói cước chưa có biểu giá | `ActivePrice = null` | `Throw BadRequestException` | Passed | N/A |
| `SetPlanStatusAsync_WhenActivatingWithoutFeatures_ShouldThrowBadRequest` | Chặn bật bán gói rỗng lợi ích | `Features.Count = 0` | `Throw BadRequestException` | Passed | N/A |
| `SetPlanStatusAsync_WhenActivatingWithValidData_ShouldCreateStripeProductAndPrice` | Test cơ chế đồng bộ tạo Stripe Product ngay khi bật Active Plan | Plan hợp lệ | Sync Stripe -> Trả về Entity chứa `StripeProductId` | Passed | N/A |

### 1.17 Subscription Service (Billing)
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế | Lỗi/Log Message |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `EnsureFreeSubscriptionAsync_WhenAlreadyActive_ShouldReturn` | Tránh spam tạo Free Plan rác nếu user đã có | User Has Active | `Returns empty` không insert DB | Passed | N/A |
| `EvaluateFeatureAccessAsync_WhenNoActivePlan_ShouldDeny` | Chặn sử dụng app khi bị expire/chưa mua mạc định | No Active Sub | `Access Denied (NoActiveSubscription)` | Passed | N/A |
| `EvaluateFeatureAccessAsync_WhenFeatureNotIncluded_ShouldDeny` | Chặn user gọi tính năng ngoài gói đang mua | FeatureCode không map | `Access Denied (FeatureNotInPlan)` | Passed | N/A |
| `EvaluateFeatureAccessAsync_WhenLimitReached_ShouldDeny` | Chặn vượt quá Quota giới hạn (VD: scan PDF bill) | `UsedCount >= Limit` | `Access Denied (UsageLimitReached)` | Passed | N/A |
| `CreateCheckoutSessionAsync_WhenFreePlan_ShouldThrowBadRequest` | Chống khai thác kẽ hở gen checkout Link đi thanh toán cho Plan Free | `BasePrice = 0` | `Throw BadRequestException` | Passed | N/A |

### 1.18 Smoke Test — Toàn bộ Service Constructible & Invokable
| Test Case | Mục đích | Đầu vào (Input) | Output mong đợi | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| `All_Service_Classes_Should_Be_Constructible` | Đảm bảo mọi Service class đều khởi tạo được (không lỗi DI thiếu) | Reflection scan toàn bộ `*Service` Assembly | Tất cả `NotNull` | Passed |
| `All_Application_And_Infrastructure_Service_Methods_Should_Be_Invoked` | Smoke test gọi thử mọi public method (chấp nhận throw) | Reflection invoke + mock params | `invokedMethods > 0` | Passed |

---

## 2. API Test - Lớp Controller (Trong tương lai)

Phần này dùng để cung cấp cấu trúc mẫu cho các Integration/Controller Test, nơi thực thi xác nhận tính đúng đắn của giao tiếp luồng HTTP Request/Response tới các Entity Service, Validation JSON Body và Identity Authorization Headers. **(Phần này sẽ được triển khai mở rộng ở đợt sau)**

| API Name / Controller | Mục đích test (Chính) | Đầu vào (Input) | Output dự kiến (HTTP State) | Thực tế |
| :--- | :--- | :--- | :--- | :--- |
| **`POST /api/auth/google`** | Test đăng nhập và tạo mới Access JWT | `{ token: "jwt_string" }` | `200 OK` (hoặc `400 Bad Request`) | `[TBD]` |
| **`GET /api/profile`** | Truy xuất với Bearer Authorization | valid `Authentication Bearer` | `200 OK` ra Profile | `[TBD]` |
| **`POST /api/locations`** | Tạo chi nhánh với Model Binding Regex Validations | Model lỗi address size 0 | `422 Unprocessable Entity` | `[TBD]` |
| **`GET /api/inventory/{id}`** | Xác thực Path Parameter Parsing Id | Query Param ko hợp lệ / 404 | `404 Not Found` | `[TBD]` |
| **`POST /api/accounting/forms`**| Gửi Payload rất lớn chứa sổ kế toán Array | 5MB Configured Limit | `413 Payload Too Large` or `200 OK` | `[TBD]` |

---
*Ghi chú: Bản báo cáo này được cập nhật để đối chiếu chính xác từng case đã được lập trình dưới tầng Mock Repository thay vì các con số tổng lượng. Ở giai đoạn Unit test, Service đã chạy đủ Coverage các nhánh lớn.*
