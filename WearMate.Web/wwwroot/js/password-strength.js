document.addEventListener("DOMContentLoaded", function () {
    const input = document.getElementById("passwordInput");
    const strength = document.getElementById("password-strength");

    if (!input || !strength) return;

    input.addEventListener("input", function () {
        const val = input.value;

        let score = 0;

        if (val.length >= 6) score++;
        if (/[A-Z]/.test(val)) score++;
        if (/[a-z]/.test(val)) score++;
        if (/\d/.test(val)) score++;
        if (/[@$!%*?&#]/.test(val)) score++;

        switch (score) {
            case 0:
            case 1:
                strength.textContent = "Weak password";
                strength.style.color = "#e74c3c";
                break;
            case 2:
                strength.textContent = "Fair password";
                strength.style.color = "#f39c12";
                break;
            case 3:
                strength.textContent = "Good password";
                strength.style.color = "#2980b9";
                break;
            case 4:
            case 5:
                strength.textContent = "Strong password";
                strength.style.color = "#27ae60";
                break;
        }
    });
});
