/**
 * OrcPharm - Signup Process JavaScript
 * Handles registration, verification, and payment flow
 */

// ============================================
// SIGNUP FORM - STEP 1: REGISTRATION
// ============================================
document.addEventListener('DOMContentLoaded', function() {
    const signupForm = document.getElementById('signupForm');
    
    if (signupForm) {
        // Real-time CNPJ validation
        const cnpjInput = document.getElementById('cnpj');
        if (cnpjInput) {
            cnpjInput.addEventListener('input', function(e) {
                this.value = formatCNPJ(this.value);
                validateCNPJField(this);
            });
        }

        // Real-time WhatsApp validation
        const whatsappInput = document.getElementById('whatsapp');
        if (whatsappInput) {
            whatsappInput.addEventListener('input', function(e) {
                this.value = formatPhone(this.value);
                validatePhoneField(this);
            });
        }

        // Password strength indicator
        const passwordInput = document.getElementById('password');
        const passwordConfirm = document.getElementById('passwordConfirm');
        
        if (passwordInput) {
            passwordInput.addEventListener('input', function() {
                updatePasswordStrength(this.value);
            });
        }

        if (passwordConfirm) {
            passwordConfirm.addEventListener('input', function() {
                validatePasswordMatch();
            });
        }

        // Form submission
        signupForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            
            if (!validateSignupForm()) {
                return;
            }

            const submitBtn = this.querySelector('button[type="submit"]');
            const originalText = submitBtn.innerHTML;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Processando...';
            submitBtn.disabled = true;

            const formData = {
                nomeFantasia: document.getElementById('nomeFantasia').value.trim(),
                razaoSocial: document.getElementById('razaoSocial').value.trim(),
                cnpj: document.getElementById('cnpj').value.replace(/\D/g, ''),
                whatsApp: document.getElementById('whatsapp').value.replace(/\D/g, ''),
                email: document.getElementById('email').value.trim(),
                planId: document.getElementById('planId').value,
                password: document.getElementById('password').value,
                acceptTerms: document.getElementById('acceptTerms').checked
            };

            try {
                const response = await fetch('/api/signup/register', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(formData)
                });

                const data = await response.json();

                if (response.ok) {
                    // Store for next step
                    sessionStorage.setItem('signupWhatsApp', formData.whatsApp);
                    sessionStorage.setItem('signupEstablishmentId', data.establishmentId);
                    
                    showAlert('success', 'Cadastro realizado! Enviamos um código para seu WhatsApp.');
                    
                    // Redirect to verification
                    setTimeout(() => {
                        window.location.href = `/signup/verify?whatsapp=${formData.whatsApp}`;
                    }, 1500);
                } else {
                    showAlert('danger', data.message || 'Erro ao processar cadastro.');
                }
            } catch (error) {
                console.error('Signup error:', error);
                showAlert('danger', 'Erro ao conectar com o servidor. Tente novamente.');
            } finally {
                submitBtn.innerHTML = originalText;
                submitBtn.disabled = false;
            }
        });
    }
});

// ============================================
// VERIFICATION FORM - STEP 2: CODE VERIFICATION
// ============================================
document.addEventListener('DOMContentLoaded', function() {
    const verifyForm = document.getElementById('verifyForm');
    
    if (verifyForm) {
        const codeInputs = document.querySelectorAll('.code-input');
        
        // Auto-focus on next input
        codeInputs.forEach((input, index) => {
            input.addEventListener('input', function(e) {
                if (this.value.length === 1 && index < codeInputs.length - 1) {
                    codeInputs[index + 1].focus();
                }
                
                // Auto-submit when all 6 digits are filled
                if (index === codeInputs.length - 1 && this.value.length === 1) {
                    const code = Array.from(codeInputs).map(inp => inp.value).join('');
                    if (code.length === 6) {
                        submitVerificationCode(code);
                    }
                }
            });

            // Handle backspace
            input.addEventListener('keydown', function(e) {
                if (e.key === 'Backspace' && !this.value && index > 0) {
                    codeInputs[index - 1].focus();
                    codeInputs[index - 1].value = '';
                }
            });

            // Allow only numbers
            input.addEventListener('keypress', function(e) {
                if (!/[0-9]/.test(e.key)) {
                    e.preventDefault();
                }
            });
        });

        // Manual form submission
        verifyForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const code = Array.from(codeInputs).map(inp => inp.value).join('');
            submitVerificationCode(code);
        });

        // Resend code button
        const resendBtn = document.getElementById('resendCode');
        if (resendBtn) {
            resendBtn.addEventListener('click', async function(e) {
                e.preventDefault();
                
                const whatsapp = new URLSearchParams(window.location.search).get('whatsapp');
                if (!whatsapp) {
                    showAlert('danger', 'WhatsApp não encontrado. Volte ao cadastro.');
                    return;
                }

                this.disabled = true;
                this.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Enviando...';

                try {
                    const response = await fetch('/api/signup/resend-code', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ whatsApp: whatsapp })
                    });

                    if (response.ok) {
                        showAlert('success', 'Novo código enviado para seu WhatsApp!');
                        startResendCooldown();
                    } else {
                        showAlert('danger', 'Erro ao reenviar código. Tente novamente.');
                    }
                } catch (error) {
                    showAlert('danger', 'Erro ao conectar com o servidor.');
                } finally {
                    this.disabled = false;
                    this.textContent = 'Reenviar código';
                }
            });

            startResendCooldown();
        }
    }
});

async function submitVerificationCode(code) {
    if (code.length !== 6) {
        showAlert('warning', 'Por favor, digite o código de 6 dígitos.');
        return;
    }

    const whatsapp = new URLSearchParams(window.location.search).get('whatsapp');
    if (!whatsapp) {
        showAlert('danger', 'WhatsApp não encontrado. Volte ao cadastro.');
        return;
    }

    const verifyBtn = document.querySelector('#verifyForm button[type="submit"]');
    if (verifyBtn) {
        verifyBtn.disabled = true;
        verifyBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Verificando...';
    }

    try {
        const response = await fetch('/api/signup/verify', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ code, whatsApp: whatsapp })
        });

        const data = await response.json();

        if (response.ok) {
            showAlert('success', 'Código verificado com sucesso!');
            
            setTimeout(() => {
                const planId = sessionStorage.getItem('signupPlanId') || new URLSearchParams(window.location.search).get('planId');
                const establishmentId = sessionStorage.getItem('signupEstablishmentId');
                window.location.href = `/signup/payment?establishmentId=${establishmentId}&planId=${planId}`;
            }, 1000);
        } else {
            showAlert('danger', data.message || 'Código inválido ou expirado.');
            clearCodeInputs();
        }
    } catch (error) {
        console.error('Verification error:', error);
        showAlert('danger', 'Erro ao verificar código. Tente novamente.');
    } finally {
        if (verifyBtn) {
            verifyBtn.disabled = false;
            verifyBtn.textContent = 'Verificar';
        }
    }
}

function clearCodeInputs() {
    const codeInputs = document.querySelectorAll('.code-input');
    codeInputs.forEach(input => input.value = '');
    codeInputs[0].focus();
}

function startResendCooldown() {
    const resendBtn = document.getElementById('resendCode');
    if (!resendBtn) return;

    let seconds = 60;
    resendBtn.disabled = true;

    const interval = setInterval(() => {
        seconds--;
        resendBtn.textContent = `Reenviar em ${seconds}s`;

        if (seconds <= 0) {
            clearInterval(interval);
            resendBtn.disabled = false;
            resendBtn.textContent = 'Reenviar código';
        }
    }, 1000);
}

// ============================================
// VALIDATION FUNCTIONS
// ============================================

function validateSignupForm() {
    let isValid = true;
    const errors = [];

    // Nome Fantasia
    const nomeFantasia = document.getElementById('nomeFantasia');
    if (!nomeFantasia.value.trim() || nomeFantasia.value.trim().length < 3) {
        setFieldError(nomeFantasia, 'Nome fantasia deve ter pelo menos 3 caracteres');
        isValid = false;
    } else {
        clearFieldError(nomeFantasia);
    }

    // CNPJ
    const cnpj = document.getElementById('cnpj');
    if (!isValidCNPJ(cnpj.value)) {
        setFieldError(cnpj, 'CNPJ inválido');
        isValid = false;
    } else {
        clearFieldError(cnpj);
    }

    // WhatsApp
    const whatsapp = document.getElementById('whatsapp');
    if (!isValidPhone(whatsapp.value)) {
        setFieldError(whatsapp, 'WhatsApp inválido (10 ou 11 dígitos)');
        isValid = false;
    } else {
        clearFieldError(whatsapp);
    }

    // Email
    const email = document.getElementById('email');
    if (!isValidEmail(email.value)) {
        setFieldError(email, 'E-mail inválido');
        isValid = false;
    } else {
        clearFieldError(email);
    }

    // Password
    const password = document.getElementById('password');
    const passwordErrors = validatePassword(password.value);
    if (passwordErrors.length > 0) {
        setFieldError(password, passwordErrors[0]);
        isValid = false;
    } else {
        clearFieldError(password);
    }

    // Password Confirm
    if (!validatePasswordMatch()) {
        isValid = false;
    }

    // Terms
    const acceptTerms = document.getElementById('acceptTerms');
    if (!acceptTerms.checked) {
        showAlert('warning', 'Você deve aceitar os termos de uso.');
        isValid = false;
    }

    return isValid;
}

function isValidCNPJ(cnpj) {
    cnpj = cnpj.replace(/\D/g, '');
    
    if (cnpj.length !== 14) return false;
    if (/^(\d)\1{13}$/.test(cnpj)) return false;

    let size = cnpj.length - 2;
    let numbers = cnpj.substring(0, size);
    let digits = cnpj.substring(size);
    let sum = 0;
    let pos = size - 7;

    for (let i = size; i >= 1; i--) {
        sum += numbers.charAt(size - i) * pos--;
        if (pos < 2) pos = 9;
    }

    let result = sum % 11 < 2 ? 0 : 11 - (sum % 11);
    if (result != digits.charAt(0)) return false;

    size = size + 1;
    numbers = cnpj.substring(0, size);
    sum = 0;
    pos = size - 7;

    for (let i = size; i >= 1; i--) {
        sum += numbers.charAt(size - i) * pos--;
        if (pos < 2) pos = 9;
    }

    result = sum % 11 < 2 ? 0 : 11 - (sum % 11);
    if (result != digits.charAt(1)) return false;

    return true;
}

function isValidPhone(phone) {
    const cleaned = phone.replace(/\D/g, '');
    return cleaned.length === 10 || cleaned.length === 11;
}

function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function validatePassword(password) {
    const errors = [];
    
    if (password.length < 8) {
        errors.push('Senha deve ter pelo menos 8 caracteres');
    }
    if (!/[A-Z]/.test(password)) {
        errors.push('Senha deve conter pelo menos uma letra maiúscula');
    }
    if (!/[a-z]/.test(password)) {
        errors.push('Senha deve conter pelo menos uma letra minúscula');
    }
    if (!/[0-9]/.test(password)) {
        errors.push('Senha deve conter pelo menos um número');
    }
    if (!/[!?*\.@#$%]/.test(password)) {
        errors.push('Senha deve conter pelo menos um caractere especial (!?*.@#$%)');
    }
    
    return errors;
}

function validatePasswordMatch() {
    const password = document.getElementById('password');
    const passwordConfirm = document.getElementById('passwordConfirm');
    
    if (password.value !== passwordConfirm.value) {
        setFieldError(passwordConfirm, 'As senhas não coincidem');
        return false;
    } else {
        clearFieldError(passwordConfirm);
        return true;
    }
}

function updatePasswordStrength(password) {
    const strengthBar = document.getElementById('passwordStrength');
    const strengthText = document.getElementById('passwordStrengthText');
    
    if (!strengthBar || !strengthText) return;

    let strength = 0;
    
    if (password.length >= 8) strength++;
    if (password.length >= 12) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[!?*\.@#$%]/.test(password)) strength++;

    const percentage = (strength / 6) * 100;
    strengthBar.style.width = percentage + '%';

    if (percentage < 40) {
        strengthBar.className = 'progress-bar bg-danger';
        strengthText.textContent = 'Fraca';
        strengthText.className = 'text-danger small';
    } else if (percentage < 70) {
        strengthBar.className = 'progress-bar bg-warning';
        strengthText.textContent = 'Média';
        strengthText.className = 'text-warning small';
    } else {
        strengthBar.className = 'progress-bar bg-success';
        strengthText.textContent = 'Forte';
        strengthText.className = 'text-success small';
    }
}

function validateCNPJField(field) {
    if (isValidCNPJ(field.value)) {
        field.classList.remove('is-invalid');
        field.classList.add('is-valid');
    } else {
        field.classList.remove('is-valid');
        if (field.value.length === 18) { // Fully formatted
            field.classList.add('is-invalid');
        }
    }
}

function validatePhoneField(field) {
    const cleaned = field.value.replace(/\D/g, '');
    if (cleaned.length === 10 || cleaned.length === 11) {
        field.classList.remove('is-invalid');
        field.classList.add('is-valid');
    } else {
        field.classList.remove('is-valid');
        if (cleaned.length > 0) {
            field.classList.add('is-invalid');
        }
    }
}

// ============================================
// FORMATTING FUNCTIONS
// ============================================

function formatCNPJ(value) {
    value = value.replace(/\D/g, '');
    value = value.substring(0, 14);
    
    if (value.length > 12) {
        value = value.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})/, '$1.$2.$3/$4-$5');
    } else if (value.length > 8) {
        value = value.replace(/^(\d{2})(\d{3})(\d{3})(\d{0,4})/, '$1.$2.$3/$4');
    } else if (value.length > 5) {
        value = value.replace(/^(\d{2})(\d{3})(\d{0,3})/, '$1.$2.$3');
    } else if (value.length > 2) {
        value = value.replace(/^(\d{2})(\d{0,3})/, '$1.$2');
    }
    
    return value;
}

function formatPhone(value) {
    value = value.replace(/\D/g, '');
    value = value.substring(0, 11);
    
    if (value.length > 10) {
        value = value.replace(/^(\d{2})(\d{5})(\d{4})/, '($1) $2-$3');
    } else if (value.length > 6) {
        value = value.replace(/^(\d{2})(\d{4})(\d{0,4})/, '($1) $2-$3');
    } else if (value.length > 2) {
        value = value.replace(/^(\d{2})(\d{0,5})/, '($1) $2');
    }
    
    return value;
}

// ============================================
// UI HELPERS
// ============================================

function setFieldError(field, message) {
    field.classList.add('is-invalid');
    field.classList.remove('is-valid');
    
    let feedback = field.nextElementSibling;
    if (!feedback || !feedback.classList.contains('invalid-feedback')) {
        feedback = document.createElement('div');
        feedback.className = 'invalid-feedback';
        field.parentNode.insertBefore(feedback, field.nextSibling);
    }
    feedback.textContent = message;
}

function clearFieldError(field) {
    field.classList.remove('is-invalid');
    const feedback = field.nextElementSibling;
    if (feedback && feedback.classList.contains('invalid-feedback')) {
        feedback.textContent = '';
    }
}

function showAlert(type, message) {
    const existingAlerts = document.querySelectorAll('.alert-floating');
    existingAlerts.forEach(alert => alert.remove());
    
    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-floating alert-dismissible fade show`;
    alert.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(alert);
    
    setTimeout(() => {
        alert.classList.remove('show');
        setTimeout(() => alert.remove(), 300);
    }, 5000);
}
