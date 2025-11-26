// Simple shared helpers for slug generation across admin forms
// Exposes window.slugHelper with slugify + attach utilities
(function () {
    const slugHelper = {
        slugify(text, fallback) {
            const source = text && text.toString().trim().length > 0
                ? text.toString()
                : (fallback || "");

            const normalized = source
                .toLowerCase()
                .normalize("NFKD")
                .replace(/[\u0300-\u036f]/g, "")
                .replace(/[^a-z0-9\s-]/g, "")
                .trim();

            if (!normalized) return fallback || "";

            return normalized
                .replace(/\s+/g, "-")
                .replace(/-+/g, "-");
        },

        attachSlugAuto(nameInput, slugInput, options = {}) {
            if (!nameInput || !slugInput) return;
            const respectManual = options.respectManual ?? true;
            const forceWhenEmpty = options.forceWhenEmpty ?? true;

            const markManual = () => {
                if (!respectManual) return;
                if (slugInput.value) {
                    slugInput.dataset.slugManual = "1";
                } else {
                    delete slugInput.dataset.slugManual;
                }
            };

            slugInput.addEventListener("input", markManual);

            const update = () => {
                if (!forceWhenEmpty && slugInput.value) return;
                if (respectManual && slugInput.dataset.slugManual === "1" && slugInput.value) return;
                slugInput.value = slugHelper.slugify(nameInput.value || "");
            };

            nameInput.addEventListener("input", update);
        },

        attachSlugFromFileInput(fileInput, slugInput, fallbackInput) {
            if (!fileInput || !slugInput) return;
            fileInput.addEventListener("change", () => {
                if (slugInput.value) return;
                const file = fileInput.files && fileInput.files[0];
                const baseName = file ? file.name.split(".").slice(0, -1).join(".") : "";
                const source = baseName || (fallbackInput?.value ?? "");
                slugInput.value = slugHelper.slugify(source);
            });
        },

        attachSlugFromButton(button, fileInput, slugInput, fallbackInput) {
            if (!button || !slugInput) return;
            button.addEventListener("click", () => {
                const file = fileInput && fileInput.files && fileInput.files[0];
                const baseName = file ? file.name.split(".").slice(0, -1).join(".") : "";
                const source = baseName || (fallbackInput?.value ?? "");
                slugInput.value = slugHelper.slugify(source);
                delete slugInput.dataset.slugManual;
            });
        }
    };

    window.slugHelper = slugHelper;
})();
