# Admin Manage Users - Visual Guide & Code Examples

## huhuhuhuhuhu help me Lord

## User Interface Overview

### User Management Table
```
┌─────────────────────────────────────────────────────────────────────┐
│ User Management                                                     │
├──────────┬──────────────┬─────────────┬──────────────┬──────────────┤
│ Username │ Institutional│   Role      │    Email     │   Status    │
│          │     ID       │             │              │             │
├──────────┼──────────────┼─────────────┼──────────────┼─────────────┤
│ john.doe │ 123456789    │ Educator    │ john@uni.edu │ ✓ Active   │
│          │              │             │              │ [Actions ▼] │
├──────────┼──────────────┼─────────────┼──────────────┼─────────────┤
│ jane.sm  │ 987654321    │ Admin       │ jane@uni.edu │ ⏸ Suspended│
│          │              │             │              │ [Actions ▼] │
├──────────┼──────────────┼─────────────┼──────────────┼─────────────┤
│ bob.wils │ 555666777    │ Dept Head   │ bob@uni.edu  │ ⏳ Pending  │
│          │              │             │              │ [Actions ▼] │
└──────────┴──────────────┴─────────────┴──────────────┴─────────────┘
```

---

## Dropdown Menu States

### For Active User
```
[Actions ▼]
	├─ Deactivate
	├─ ─────────
	└─ Delete
```

### For Inactive User (Suspended/PendingApproval/Rejected)
```
[Actions ▼]
	├─ Activate
	├─ ─────────
	└─ Delete
```

### For Deleted User
```
[Actions ▼]
	└─ Delete
```

---

## Confirmation Modal Examples

### Activate User Modal
```
╔════════════════════════════════════════╗
║         Activate User               ✕  ║
╠════════════════════════════════════════╣
║                                        ║
║  User:              john.doe           ║
║  Email:             john@uni.edu       ║
║  Institutional ID:  123456789          ║
║  Current Status:    [Suspended]        ║
║  ───────────────────────────────────   ║
║                                        ║
║  Activate user 'john.doe'? They will  ║
║  be able to sign in again.            ║
║                                        ║
╠════════════════════════════════════════╣
║  [Cancel]              [✓ Confirm]     ║
╚════════════════════════════════════════╝
```

### Deactivate User Modal
```
╔════════════════════════════════════════╗
║         Deactivate User             ✕  ║
╠════════════════════════════════════════╣
║                                        ║
║  User:              john.doe           ║
║  Email:             john@uni.edu       ║
║  Institutional ID:  123456789          ║
║  Current Status:    [Active] ✓         ║
║  ───────────────────────────────────   ║
║                                        ║
║  Deactivate user 'john.doe'? They     ║
║  will not be able to sign in.         ║
║                                        ║
╠════════════════════════════════════════╣
║  [Cancel]              [⚠ Confirm]     ║
╚════════════════════════════════════════╝
```

### Delete User Modal
```
╔════════════════════════════════════════╗
║           Delete User               ✕  ║
╠════════════════════════════════════════╣
║                                        ║
║  User:              john.doe           ║
║  Email:             john@uni.edu       ║
║  Institutional ID:  123456789          ║
║  Current Status:    [Active] ✓         ║
║  ───────────────────────────────────   ║
║                                        ║
║  Soft-delete user 'john.doe'? This    ║
║  sets status to Deleted for audit     ║
║  trail.                               ║
║                                        ║
╠════════════════════════════════════════╣
║  [Cancel]              [✗ Confirm]     ║
╚════════════════════════════════════════╝
```

### Loading State
```
╔════════════════════════════════════════╗
║           Delete User               ✕  ║
╠════════════════════════════════════════╣
║  [... user details ...]                ║
╠════════════════════════════════════════╣
║  [Cancel]      [⟳ Confirm] (disabled)  ║
╚════════════════════════════════════════╝
```

---

## Code Examples

### Using UserActionService

#### Get All Users
```csharp
var users = await UserActionService.GetAllUsersAsync();
```

#### Update User Status
```csharp
var updated = await UserActionService.UpdateUserStatusAsync(
	userId: "user-123",
	newStatus: UserAccountStatus.Active
);
```

#### Check if Action is Allowed
```csharp
var user = users.First();
bool allowed = UserActionService.IsActionAllowed(
	user: user,
	targetStatus: UserAccountStatus.Active
);
```

---

### Using ConfirmationModal in Razor

#### In UserManagement.razor
```razor
@page "/admin/user-management"
@inject UserActionService UserActionService

<ConfirmationModal 
	IsVisible="showConfirmModal" 
	IsProcessing="isProcessing"
	SelectedUser="pendingUser"
	TargetStatus="pendingStatus ?? UserAccountStatus.Active"
	OnCancel="CloseConfirmationAsync"
	OnConfirm="ApplyConfirmedActionAsync" />

@code {
	private bool showConfirmModal;
	private bool isProcessing;
	private ApplicationUser? pendingUser;
	private UserAccountStatus? pendingStatus;

	private void OpenConfirmation(ApplicationUser user, UserAccountStatus status)
	{
		pendingUser = user;
		pendingStatus = status;
		showConfirmModal = true;
	}

	private async Task ApplyConfirmedActionAsync()
	{
		if (pendingUser is null || pendingStatus is null)
			return;

		isProcessing = true;
		try
		{
			await UserActionService.UpdateUserStatusAsync(
				pendingUser.Id, 
				pendingStatus.Value
			);
			// Refresh list, show success message
		}
		finally
		{
			isProcessing = false;
		}
	}
}
```

---

### Conditional Dropdown Rendering

```razor
<ul class="dropdown-menu">
	@* Activate - only for non-active users *@
	@if (user.AccountStatus != UserAccountStatus.Active)
	{
		<li>
			<button class="dropdown-item" 
					@onclick="() => OpenConfirmation(user, UserAccountStatus.Active)">
				Activate
			</button>
		</li>
	}

	@* Deactivate - only for active users *@
	@if (user.AccountStatus == UserAccountStatus.Active)
	{
		<li>
			<button class="dropdown-item" 
					@onclick="() => OpenConfirmation(user, UserAccountStatus.Suspended)">
				Deactivate
			</button>
		</li>
	}

	@* Divider and Delete - always visible *@
	<li><hr class="dropdown-divider" /></li>
	<li>
		<button class="dropdown-item text-danger" 
				@onclick="() => OpenConfirmation(user, UserAccountStatus.Deleted)">
			Delete
		</button>
	</li>
</ul>
```

---

## Component Parameter Details

### ConfirmationModal Parameters

```csharp
[Parameter]
public bool IsVisible { get; set; }
// Controls whether modal is displayed
// true = show, false = hide

[Parameter]
public bool IsProcessing { get; set; }
// Disables buttons and shows loading spinner
// true = action in progress, false = ready for interaction

[Parameter]
public ApplicationUser? SelectedUser { get; set; }
// The user being acted upon
// Null = no user selected

[Parameter]
public UserAccountStatus TargetStatus { get; set; }
// The status to change to (Active, Suspended, Deleted)
// Determines title, message, and button color

[Parameter]
public EventCallback OnCancel { get; set; }
// Called when user clicks Cancel

[Parameter]
public EventCallback OnConfirm { get; set; }
// Called when user clicks Confirm
```

---

## Database Operations Flow

### Update User Status
```
UserActionService.UpdateUserStatusAsync(userId, newStatus)
	↓
Get DbContext from factory
	↓
Find user by ID
	↓
Update AccountStatus property
	↓
Call SaveChangesAsync()
	↓
Return updated user
```

### Get All Users
```
UserActionService.GetAllUsersAsync()
	↓
Get DbContext from factory
	↓
Query Users with AsNoTracking
	↓
Order by UserName
	↓
Convert to List
	↓
Return user list
```

---

## Status Badge Styling

```razor
@code {
	private static string GetStatusBadgeClass(UserAccountStatus status) => status switch
	{
		UserAccountStatus.Active => "text-bg-success",
		UserAccountStatus.PendingApproval => "text-bg-warning text-dark",
		UserAccountStatus.Suspended => "text-bg-secondary",
		UserAccountStatus.Rejected => "text-bg-danger",
		UserAccountStatus.Deleted => "text-bg-dark",
		_ => "text-bg-secondary"
	};
}
```

**Resulting Badges**:
- `<span class="badge text-bg-success">Active</span>` → Green
- `<span class="badge text-bg-warning text-dark">PendingApproval</span>` → Yellow
- `<span class="badge text-bg-secondary">Suspended</span>` → Gray
- `<span class="badge text-bg-danger">Rejected</span>` → Red
- `<span class="badge text-bg-dark">Deleted</span>` → Dark

---

## Success Messages

After action completes:

```
✓ User 'john.doe' was activated and saved to the database.
✓ User 'jane.smith' was deactivated and saved to the database.
✓ User 'bob.wilson' was deleted and saved to the database.
```

Displayed in green alert box:
```
┌─────────────────────────────────────────────────────┐
│ ✓ User 'john.doe' was activated and saved to the   │
│   database.                                         │
└─────────────────────────────────────────────────────┘
```

---

## State Transitions

```
┌──────────────┐
│  PendingAppr │  ──Activate──→  ┌────────┐
└──────────────┘                 │ Active │
								 └────────┘
┌──────────┐                           │
│Suspended │  ←───Deactivate───┐      │
└──────────┘                   │      │
							  ┌┴──────┴──┐
┌─────────┐                  │ Rejected │
│ Deleted │  ←────Delete─────└──────────┘
└─────────┘
```

Any status can transition to Deleted (soft-delete).

---

## Error Handling

### Current Implementation
```csharp
try
{
	var updatedUser = await UserActionService.UpdateUserStatusAsync(
		pendingUser.Id, 
		pendingStatus.Value
	);

	if (updatedUser is null)
	{
		await CloseConfirmationAsync();
		return;
	}

	users = await UserActionService.GetAllUsersAsync();
	successMessage = "User updated successfully.";
}
finally
{
	isProcessing = false;
}
```

### User Not Found
If user doesn't exist when updating:
- Modal closes silently
- List refreshes
- No error displayed

### Future Enhancement
Consider adding error display:
```csharp
private string? errorMessage;

// In modal:
@if (!string.IsNullOrWhiteSpace(errorMessage))
{
	<div class="alert alert-danger">@errorMessage</div>
}
```

---

## Loading Indicators

### Button Loading State
```razor
<button type="button" 
		class="btn @ConfirmButtonClass" 
		@onclick="OnConfirm" 
		disabled="@IsProcessing">
	@if (IsProcessing)
	{
		<span class="spinner-border spinner-border-sm me-2" 
			  role="status" 
			  aria-hidden="true"></span>
	}
	Confirm
</button>
```

**Result**:
- When IsProcessing = true: Shows spinner + text, button disabled
- When IsProcessing = false: Just text, button enabled

---

## Architecture Diagram

```
┌─────────────────────────────────────┐
│      UserManagement.razor           │
│   (UI Layer - List & Dropdown)      │
└──────────────────┬──────────────────┘
				   │
				   │ Calls
				   ↓
	┌──────────────────────────────┐
	│  ConfirmationModal.razor      │
	│  (Component Layer - Modal UI) │
	└──────────────┬───────────────┘
				   │
				   │ Callbacks
				   ↓
┌──────────────────────────────────────┐
│     UserActionService                │
│  (Service Layer - Business Logic)    │
│                                      │
│  • UpdateUserStatusAsync()           │
│  • GetAllUsersAsync()                │
│  • IsActionAllowed()                 │
└──────────────────┬───────────────────┘
				   │
				   │ Uses
				   ↓
		 ┌──────────────────┐
		 │  ApplicationDb   │
		 │   Context        │
		 │ (Database Layer) │
		 └──────────────────┘
```

---

## Reusability Examples

### Example 1: Department User Management
```razor
@page "/admin/departments/{deptId}/users"

<ConfirmationModal 
	IsVisible="showModal"
	IsProcessing="isProcessing"
	SelectedUser="selectedUser"
	TargetStatus="targetStatus"
	OnCancel="@(() => showModal = false)"
	OnConfirm="@ApplyActionAsync" />
```

### Example 2: Role Management
```razor
@page "/admin/roles"

<ConfirmationModal 
	IsVisible="showRoleModal"
	IsProcessing="isProcessing"
	SelectedUser="selectedUser"
	TargetStatus="targetStatus"
	OnCancel="@(() => showRoleModal = false)"
	OnConfirm="@UpdateRoleAsync" />
```

---

## Testing Scenarios

### Test 1: Activate Inactive User
```
Given: User with status = "Suspended"
When: Admin clicks "Activate"
Then: Modal shows "Activate User"
	  Modal shows [Success] button
	  After confirm: User status = "Active"
```

### Test 2: Deactivate Active User
```
Given: User with status = "Active"
When: Admin clicks "Deactivate"
Then: Modal shows "Deactivate User"
	  Modal shows [Warning] button
	  After confirm: User status = "Suspended"
```

### Test 3: Delete Any User
```
Given: User with any status
When: Admin clicks "Delete"
Then: Modal shows "Delete User"
	  Modal shows [Danger] button
	  After confirm: User status = "Deleted"
```

### Test 4: Loading State
```
Given: Action is being processed
When: Confirm button is being processed
Then: Spinner shows in button
	  Button is disabled
	  Modal buttons disabled
```

---

End of Visual Guide ✨
