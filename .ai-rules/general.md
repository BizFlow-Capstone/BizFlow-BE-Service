# BizFlow - General Coding Rules

## 🎯 Project Context

BizFlow is a platform supporting digital transformation for household businesses in Vietnam.
The target users have low digital literacy and primarily use smartphones.

## 👥 User Roles

| Role | Permissions |
|------|-------------|

| **User** | CRUD orders, products, inventory, debts, view reports |
| **Admin** | Manage accounts, pricing, system config, platform analytics |
| **Consultant** | Manage financial report templates, notifications |

## Naming Conventions

### General

- **PascalCase**: Classes, Methods, Properties, Enums
- **camelCase**: Local variables, parameters, private fields
- **SCREAMING_SNAKE_CASE**: Constants
- **PascalCase**: Database tables, columns

### File Names

| Type | Format | Example |
|------|--------|---------|

| Entity | `{Name}.cs` | `Order.cs` |
| DTO | `{Name}Dto.cs` | `OrderDto.cs` |
| Request | `{Action}{Entity}Request.cs` | `CreateOrderRequest.cs` |
| Response | `{Entity}Response.cs` | `OrderResponse.cs` |
| Service | `{Feature}Service.cs` | `OrderService.cs` |
| Interface | `I{Name}.cs` | `IOrderService.cs` |
| Controller | `{Feature}Controller.cs` | `OrderController.cs` |
| Repository | `{Entity}Repository.cs` | `OrderRepository.cs` |

## Security Principles

- All API endpoints must have authentication (except for /auth/*)
- Validate BusinessId ownership in the service layer
- Soft delete instead of hard delete
- Do not log sensitive data (passwords, tokens, PII)

## Business Terms

- **Hộ kinh doanh**: Household business (revenue < 1B VND/year)
- **Ghi nợ**: Customer debt/credit
- **Thông tư 152**: Circular 152/2025/TT-BTC - simplified accounting
- **Draft Order**: AI-generated order pending user confirmation
