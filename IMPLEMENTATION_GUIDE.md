# Admin Manage Users - Dropdown Actions Implementation

## Overview
This document describes the refactored Admin Manage Users feature with fully functional dropdown actions (Activate, Deactivate, Delete) using reusable modal components and service-based architecture.

## Architecture

### Components

#### 1. **UserActionService** (`..\src\SRVS.Web\Components\Admin\Services\UserActionService.cs`)
A service layer handling all user account operations:

- **`UpdateUserStatusAsync(userId, newStatus)`**: Updates a user's account status in the database
- **`GetAllUsersAsync()`**: Retrieves all users ordered by username
- **`IsActionAllowed(user, targetStatus)`**: Validates if an action is permitted based on current status

**Key Features:**
- Encapsulates database operations
- Provides centralized business logic for user actions
- Can be reused across multiple pages and components

---

#### 2. **ConfirmationModal Component** (`..\src\SRVS.Web\Components\Admin\Modals\ConfirmationModal.razor`)
A reusable, generic confirmation modal component for user actions.

**Input Parameters:**
- `IsVisible` (bool): Controls modal visibility
- `IsProcessing` (bool): Displays loading state during operation
- `SelectedUser` (ApplicationUser?): User being acted upon
- `TargetStatus` (UserAccountStatus): Action target status (Active, Suspended, Deleted)

**Callbacks:**
- `OnCancel`: Called when user clicks Cancel
- `OnConfirm`: Called when user clicks Confirm

**Features:**
- Displays user information: Username, Email, Institutional ID, Current Status
- Dynamic title and message based on target status
- Conditional button styling (Success for Activate, Warning for Deactivate, Danger for Delete)
- Loading spinner during confirmation
- Displays current user status badge with appropriate color coding

**Status Badge Colors:**
- Active → Green (text-bg-success)
- PendingApproval → Yellow (text-bg-warning)
- Suspended → Gray (text-bg-secondary)
- Rejected → Red (text-bg-danger)
- Deleted → Dark (text-bg-dark)

---

#### 3. **UserManagement Page** (`..\src\SRVS.Web\Components\Admin\Pages\UserManagement.razor`)
Refactored page for managing users with dropdown actions.

**Key Changes:**
1. **Service Injection**: Uses `UserActionService` instead of direct `DbContextFactory`
2. **Simplified Dropdown Logic**:
   - Activate: Only visible for non-Active users
   - Deactivate: Only visible for Active users
   - Delete: Always visible

3. **Modal Usage**: Replaced inline modal markup with `<ConfirmationModal />` component
4. **Cleaner Code Section**: Service-based approach reduces code duplication

**State Management:**
- `users`: List of all users
- `isProcessing`: Prevents multiple simultaneous actions
- `showConfirmModal`: Controls modal visibility
- `pendingUser`: User awaiting confirmation
- `pendingStatus`: Target status for action
- `openActionsUserId`: Currently opened dropdown (only one at a time)
- `successMessage`: Feedback after successful action

---

## Dropdown Action Rules

### Activate
- **When Shown**: When user status is NOT Active
- **When Enabled**: Always enabled when shown
- **Action**: Changes user status to Active

### Deactivate
- **When Shown**: When user status IS Active
- **When Enabled**: Always enabled when shown
- **Action**: Changes user status to Suspended (inactive)

### Delete
- **When Shown**: Always visible
- **When Enabled**: Always enabled
- **Action**: Soft-deletes user (status = Deleted)

---

## User Flow

1. **View Users**: Admin navigates to `/admin/user-management`
2. **Open Dropdown**: Clicks "Actions" button for a user
   - Only one dropdown open at a time
   - Dropdown closes when action is triggered
3. **Select Action**: Clicks Activate, Deactivate, or Delete
   - Modal opens showing:
	 - User details (Username, Email, Institutional ID)
	 - Current status badge
	 - Action-specific message
	 - Appropriately styled Confirm button
4. **Confirm Action**: Admin reviews details and clicks Confirm
   - Loading spinner appears
   - Action executes
   - Modal closes
5. **View Result**: Success message displayed
   - User list automatically refreshes
   - User status updated in table

---

## Data Flow

```
UserManagement.razor
	↓
[ToggleActionsMenu] - Show/hide dropdown
	↓
[OpenConfirmation] - Validate action, open modal
	↓
ConfirmationModal Component
	↓
[ApplyConfirmedActionAsync] - Execute action
	↓
UserActionService.UpdateUserStatusAsync()
	↓
Database Update
	↓
UserActionService.GetAllUsersAsync()
	↓
[Success Message & List Refresh]
```

---

## Implementation Details

### Conditional Visibility Logic
```razor
@if (user.AccountStatus != UserAccountStatus.Active)
{
	<!-- Activate button only for inactive users -->
}

@if (user.AccountStatus == UserAccountStatus.Active)
{
	<!-- Deactivate button only for active users -->
}

<!-- Delete always visible -->
```

### Modal Invocation
```razor
<ConfirmationModal 
	IsVisible="showConfirmModal" 
	IsProcessing="isProcessing"
	SelectedUser="pendingUser"
	TargetStatus="pendingStatus ?? UserAccountStatus.Active"
	OnCancel="CloseConfirmationAsync"
	OnConfirm="ApplyConfirmedActionAsync" />
```

### Service Usage
```csharp
// Get all users
users = await UserActionService.GetAllUsersAsync();

// Check if action is allowed
if (!UserActionService.IsActionAllowed(user, targetStatus))
	return;

// Update user status
var updatedUser = await UserActionService.UpdateUserStatusAsync(userId, newStatus);
```

---

## Reusability

The `ConfirmationModal` component is fully reusable in other admin pages:

```razor
<ConfirmationModal 
	IsVisible="showModal"
	IsProcessing="isLoading"
	SelectedUser="currentUser"
	TargetStatus="targetStatus"
	OnCancel="@(() => showModal = false)"
	OnConfirm="@HandleConfirmAsync" />
```

The `UserActionService` can also be injected into other pages requiring user management:

```csharp
@inject UserActionService UserActionService
```

---

## Error Handling

- If user not found when updating: Modal closes silently
- If action fails: Currently handled with try-finally for UI state cleanup
- Future enhancement: Add error message display to modal

---

## Loading States

- **Button Disabled**: `disabled="@isProcessing"` prevents multiple submissions
- **Modal Buttons Disabled**: During action execution
- **Loading Spinner**: Displayed in Confirm button when processing

---

## Success Messages

After successful action, displays:
- Activate: "User '{UserName}' was activated and saved to the database."
- Deactivate: "User '{UserName}' was deactivated and saved to the database."
- Delete: "User '{UserName}' was deleted and saved to the database."

---

## Files Modified/Created

### Created Files
1. `..\src\SRVS.Web\Components\Admin\Services\UserActionService.cs`
2. `..\src\SRVS.Web\Components\Admin\Modals\ConfirmationModal.razor`

### Modified Files
1. `..\src\SRVS.Web\Components\Admin\Pages\UserManagement.razor`
2. `..\src\SRVS.Web\Components\_Imports.razor`
3. `..\src\SRVS.Web\Program.cs`

---

## Dependency Injection

The `UserActionService` is registered in `Program.cs`:

```csharp
builder.Services.AddScoped<UserActionService>();
```

This makes it available for injection in any scoped component or page.

---

## Testing Checklist

- [x] Activate button only shows for inactive users
- [x] Deactivate button only shows for active users
- [x] Delete button always visible
- [x] Modal displays correct user information
- [x] Modal shows action-specific messages
- [x] Modal buttons have correct styling
- [x] Action executes and updates database
- [x] List refreshes after action
- [x] Success message displays
- [x] Loading state prevents double-click
- [x] Modal closes after action
- [x] Dropdown closes when action selected
- [x] Build compiles without errors

---

## Future Enhancements

1. **Error Messages**: Display specific error messages if action fails
2. **Bulk Actions**: Select multiple users and apply actions to all
3. **Audit Logging**: Track who performed which actions and when
4. **Undo Functionality**: Allow reverting recent actions
5. **User Filters**: Filter users by status, role, department
6. **Search**: Search users by username, email, or ID

---

## UI/UX Considerations

✅ **Professional Styling**: Uses Bootstrap components consistent with existing design
✅ **Clear Messaging**: Each action has specific, clear confirmation messages
✅ **Visual Feedback**: Loading spinner and disabled states indicate processing
✅ **Accessibility**: Proper button labels and ARIA attributes
✅ **Responsive**: Works on mobile and desktop
✅ **Consistent Patterns**: Follows existing project component patterns

---

## Code Quality

✅ **Separation of Concerns**: 
  - UI Layer: UserManagement.razor
  - Modal Layer: ConfirmationModal.razor
  - Service Layer: UserActionService.cs

✅ **Reusability**: Components and services can be used in other pages

✅ **Documentation**: Comprehensive XML comments and this guide

✅ **Type Safety**: Uses enums and strong typing

✅ **Null Safety**: Proper null checks and null-coalescing operators
