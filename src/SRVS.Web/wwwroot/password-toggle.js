// Password visibility toggle functionality
document.addEventListener('DOMContentLoaded', function() {
    initializePasswordToggles();
});

// Re-initialize after Blazor updates
if (window.Blazor) {
    Blazor.addEventListener('enhanced:success', function() {
        initializePasswordToggles();
    });
}

function initializePasswordToggles() {
    const toggleButtons = document.querySelectorAll('.password-toggle-btn');

    toggleButtons.forEach(button => {
        // Remove old listeners by cloning
        const newButton = button.cloneNode(true);
        button.parentNode.replaceChild(newButton, button);

        // Add new listener
        newButton.addEventListener('click', handlePasswordToggle);
    });
}

function handlePasswordToggle(e) {
    e.preventDefault();
    e.stopPropagation();

    const button = this;
    const inputGroup = button.closest('.password-input-group');
    if (!inputGroup) return;

    // Find input field (could be input or InputText)
    let inputField = inputGroup.querySelector('input[type="password"], input[type="text"]');

    if (!inputField) return;

    // Toggle password visibility
    const currentType = inputField.getAttribute('type');
    const newType = currentType === 'password' ? 'text' : 'password';
    inputField.setAttribute('type', newType);

    // Update icon
    const icon = button.querySelector('i');
    if (icon) {
        icon.classList.remove('bi-eye-fill', 'bi-eye-slash-fill');
        icon.classList.add(newType === 'password' ? 'bi-eye-fill' : 'bi-eye-slash-fill');
    }
}

// Handle dynamically added elements
document.addEventListener('click', function(e) {
    if (e.target.closest('.password-toggle-btn')) {
        handlePasswordToggle.call(e.target.closest('.password-toggle-btn'), e);
    }
}, true);

