(() => {
    let lastFocusedElement = null;

    const modal = document.getElementById("logout-confirm");
    const backdrop = document.querySelector("[data-logout-close].logout-confirm-backdrop");

    if (!modal || !backdrop) {
        return;
    }

    const confirmButton = modal.querySelector("[data-logout-confirm]");

    const openModal = () => {
        lastFocusedElement = document.activeElement instanceof HTMLElement ? document.activeElement : null;
        modal.hidden = false;
        backdrop.hidden = false;
        document.body.classList.add("logout-confirm-open");
        window.requestAnimationFrame(() => confirmButton?.focus());
    };

    const closeModal = () => {
        modal.hidden = true;
        backdrop.hidden = true;
        document.body.classList.remove("logout-confirm-open");
        lastFocusedElement?.focus();
    };

    document.addEventListener("click", (event) => {
        const target = event.target instanceof Element ? event.target : null;

        if (target?.closest("[data-logout-open]")) {
            openModal();
            return;
        }

        if (target?.closest("[data-logout-close]")) {
            closeModal();
        }
    });

    document.addEventListener("keydown", (event) => {
        if (modal.hidden) {
            return;
        }

        if (event.key === "Escape") {
            closeModal();
            return;
        }

        if (event.key === "Tab") {
            const focusableElements = Array.from(modal.querySelectorAll("button, [href], input, select, textarea, [tabindex]:not([tabindex='-1'])"))
                .filter((element) => element instanceof HTMLElement && !element.hasAttribute("disabled"));
            const firstElement = focusableElements[0];
            const lastElement = focusableElements[focusableElements.length - 1];

            if (!firstElement || !lastElement) {
                return;
            }

            if (event.shiftKey && document.activeElement === firstElement) {
                event.preventDefault();
                lastElement.focus();
            } else if (!event.shiftKey && document.activeElement === lastElement) {
                event.preventDefault();
                firstElement.focus();
            }
        }
    });
})();
