// pdv.js

let cart = [];
let payments = [];
let selectedCustomer = null;
let currentPaymentModal = null;

document.addEventListener('DOMContentLoaded', function() {
    initializePDV();
    updateClock();
    setInterval(updateClock, 1000);
});

function initializePDV() {
    setupProductSearch();
    loadCashRegisterInfo();
    calculateTotal();
}

function updateClock() {
    const now = new Date();
    document.getElementById('currentTime').textContent = now.toLocaleString('pt-BR');
}

function setupProductSearch() {
    const searchInput = document.getElementById('productSearch');
    let timeout;

    searchInput.addEventListener('input', function() {
        clearTimeout(timeout);
        timeout = setTimeout(() => {
            if (this.value.length >= 3) {
                searchProducts(this.value);
            }
        }, 300);
    });

    searchInput.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            searchProducts(this.value);
        }
    });
}

async function searchProducts(query) {
    try {
        // Buscar produtos prontos e ordens de manipulação
        const [productsResponse, ordersResponse] = await Promise.all([
            fetch(`/api/Products/search?query=${encodeURIComponent(query)}`),
            fetch(`/api/ManipulationOrders/pending-sale?search=${encodeURIComponent(query)}`)
        ]);

        const productsResult = await productsResponse.json();
        const ordersResult = await ordersResponse.json();

        if (productsResult.success && productsResult.data.length > 0) {
            // Adicionar primeiro produto encontrado
            addToCart(productsResult.data[0]);
        } else if (ordersResult.success && ordersResult.data.length > 0) {
            // Adicionar primeira ordem encontrada
            addOrderToCart(ordersResult.data[0]);
        } else {
            showToast('Produto não encontrado', 'warning');
        }

        document.getElementById('productSearch').value = '';
        document.getElementById('productSearch').focus();
    } catch (error) {
        console.error('Erro ao buscar produtos:', error);
    }
}

function addToCart(product) {
    const existingItem = cart.find(item => item.id === product.id && item.type === 'PRODUCT');
    
    if (existingItem) {
        existingItem.quantity++;
    } else {
        cart.push({
            id: product.id,
            type: 'PRODUCT',
            description: product.name,
            unitPrice: product.price,
            quantity: 1
        });
    }
    
    renderCart();
    calculateTotal();
    showToast('Item adicionado ao carrinho', 'success');
}

function addOrderToCart(order) {
    const existingItem = cart.find(item => 
        item.id === order.id && item.type === 'MANIPULATION_ORDER'
    );
    
    if (existingItem) {
        showToast('Esta ordem já está no carrinho', 'warning');
        return;
    }
    
    cart.push({
        id: order.id,
        type: 'MANIPULATION_ORDER',
        description: order.formulaName || 'Manipulação',
        unitPrice: order.suggestedPrice || 0,
        quantity: 1
    });
    
    renderCart();
    calculateTotal();
    showToast('Ordem de manipulação adicionada', 'success');
}

function renderCart() {
    const container = document.getElementById('cartItems');
    
    if (cart.length === 0) {
        container.innerHTML = `
            <div class="text-center text-muted py-5">
                <i class="bi bi-cart-x fs-1 d-block mb-3"></i>
                <p>Carrinho vazio</p>
            </div>
        `;
        return;
    }
    
    container.innerHTML = cart.map((item, index) => `
        <div class="card mb-2">
            <div class="card-body p-2">
                <div class="d-flex justify-content-between align-items-center">
                    <div class="flex-grow-1">
                        <h6 class="mb-0">${item.description}</h6>
                        <small class="text-muted">
                            ${formatCurrency(item.unitPrice)} 
                            ${item.type === 'MANIPULATION_ORDER' ? '(Manipulação)' : ''}
                        </small>
                    </div>
                    <div class="d-flex align-items-center gap-2">
                        <button class="btn btn-sm btn-outline-secondary" onclick="decreaseQuantity(${index})">
                            <i class="bi bi-dash"></i>
                        </button>
                        <input type="number" class="form-control form-control-sm text-center" 
                               style="width: 60px;" value="${item.quantity}" min="1"
                               onchange="updateQuantity(${index}, this.value)">
                        <button class="btn btn-sm btn-outline-secondary" onclick="increaseQuantity(${index})">
                            <i class="bi bi-plus"></i>
                        </button>
                        <strong class="ms-2" style="min-width: 80px; text-align: right;">
                            ${formatCurrency(item.unitPrice * item.quantity)}
                        </strong>
                        <button class="btn btn-sm btn-outline-danger" onclick="removeFromCart(${index})">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `).join('');
}

function increaseQuantity(index) {
    cart[index].quantity++;
    renderCart();
    calculateTotal();
}

function decreaseQuantity(index) {
    if (cart[index].quantity > 1) {
        cart[index].quantity--;
        renderCart();
        calculateTotal();
    }
}

function updateQuantity(index, value) {
    const qty = parseInt(value);
    if (qty > 0) {
        cart[index].quantity = qty;
        renderCart();
        calculateTotal();
    }
}

function removeFromCart(index) {
    cart.splice(index, 1);
    renderCart();
    calculateTotal();
    showToast('Item removido', 'info');
}

function clearCart() {
    if (confirm('Deseja limpar todo o carrinho?')) {
        cart = [];
        payments = [];
        renderCart();
        renderPayments();
        calculateTotal();
    }
}

function calculateTotal() {
    const subtotal = cart.reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);
    const discountPercentage = parseFloat(document.getElementById('discountInput').value) || 0;
    const discountAmount = subtotal * (discountPercentage / 100);
    const total = subtotal - discountAmount;
    
    document.getElementById('subtotal').textContent = formatCurrency(subtotal);
    document.getElementById('discountAmount').textContent = '- ' + formatCurrency(discountAmount);
    document.getElementById('totalAmount').textContent = formatCurrency(total);
    
    updatePaymentsSummary();
}

function showPaymentModal(method) {
    if (cart.length === 0) {
        showToast('Adicione itens ao carrinho primeiro', 'warning');
        return;
    }
    
    const modal = new bootstrap.Modal(document.getElementById('paymentModal'));
    currentPaymentModal = modal;
    
    document.getElementById('paymentMethod').value = method;
    document.getElementById('paymentModalTitle').textContent = `Adicionar Pagamento - ${formatPaymentMethod(method)}`;
    
    // Limpar campos
    document.getElementById('paymentAmount').value = '';
    document.getElementById('paymentObs').value = '';
    
    // Mostrar/ocultar campos específicos
    document.querySelectorAll('[id$="Fields"]').forEach(el => el.classList.add('d-none'));
    
    if (method === 'DINHEIRO') {
        document.getElementById('cashFields').classList.remove('d-none');
        document.getElementById('cashReceived').addEventListener('input', calculateChange);
    } else if (method.includes('CARTAO')) {
        document.getElementById('cardFields').classList.remove('d-none');
        if (method === 'CARTAO_DEBITO') {
            document.getElementById('installmentsField').style.display = 'none';
        } else {
            document.getElementById('installmentsField').style.display = 'block';
        }
    } else if (method === 'PIX') {
        document.getElementById('pixFields').classList.remove('d-none');
    }
    
    // Sugerir valor restante
    const remaining = getRemainingAmount();
    if (remaining > 0) {
        document.getElementById('paymentAmount').value = remaining.toFixed(2);
    }
    
    modal.show();
    setTimeout(() => document.getElementById('paymentAmount').focus(), 500);
}

function calculateChange() {
    const amount = parseFloat(document.getElementById('paymentAmount').value) || 0;
    const received = parseFloat(document.getElementById('cashReceived').value) || 0;
    const change = received - amount;
    
    if (change >= 0) {
        document.getElementById('changeAmount').textContent = formatCurrency(change);
        document.getElementById('changeDisplay').style.display = 'block';
    } else {
        document.getElementById('changeDisplay').style.display = 'none';
    }
}

function setRemainingAmount() {
    const remaining = getRemainingAmount();
    document.getElementById('paymentAmount').value = remaining.toFixed(2);
}

function getRemainingAmount() {
    const total = parseFloat(document.getElementById('totalAmount').textContent.replace('R$', '').replace('.', '').replace(',', '.'));
    const paid = payments.reduce((sum, p) => sum + p.amount, 0);
    return Math.max(0, total - paid);
}

function addPayment() {
    const method = document.getElementById('paymentMethod').value;
    const amount = parseFloat(document.getElementById('paymentAmount').value);
    
    if (!amount || amount <= 0) {
        showToast('Informe um valor válido', 'warning');
        return;
    }
    
    const remaining = getRemainingAmount();
    if (amount > remaining) {
        showToast(`Valor excede o restante (${formatCurrency(remaining)})`, 'warning');
        return;
    }
    
    const payment = {
        method: method,
        amount: amount,
        observations: document.getElementById('paymentObs').value
    };
    
    // Adicionar campos específicos
    if (method === 'DINHEIRO') {
        payment.cashReceived = parseFloat(document.getElementById('cashReceived').value) || amount;
        payment.changeAmount = payment.cashReceived - amount;
    } else if (method.includes('CARTAO')) {
        payment.cardBrand = document.getElementById('cardBrand').value;
        payment.cardLastDigits = document.getElementById('cardLastDigits').value;
        payment.installments = parseInt(document.getElementById('installments').value);
        payment.nsu = document.getElementById('nsu').value;
        payment.authorizationCode = document.getElementById('authCode').value;
    } else if (method === 'PIX') {
        payment.pixKey = document.getElementById('pixKey').value;
        payment.pixTransactionId = document.getElementById('pixTransactionId').value;
    }
    
    payments.push(payment);
    renderPayments();
    updatePaymentsSummary();
    
    currentPaymentModal.hide();
    showToast('Pagamento adicionado', 'success');
}

function renderPayments() {
    const container = document.getElementById('paymentsContainer');
    
    if (payments.length === 0) {
        container.innerHTML = '<p class="text-muted small">Nenhum pagamento adicionado</p>';
        return;
    }
    
    container.innerHTML = payments.map((payment, index) => {
        let details = '';
        if (payment.method === 'DINHEIRO' && payment.changeAmount) {
            details = `<small class="text-muted d-block">Troco: ${formatCurrency(payment.changeAmount)}</small>`;
        } else if (payment.method.includes('CARTAO') && payment.installments) {
            details = `<small class="text-muted d-block">${payment.installments}x - ${payment.cardBrand || ''}</small>`;
        }
        
        return `
            <div class="d-flex justify-content-between align-items-center mb-2 p-2 bg-light rounded">
                <div>
                    <strong>${formatPaymentMethod(payment.method)}</strong>
                    ${details}
                </div>
                <div class="d-flex align-items-center gap-2">
                    <strong>${formatCurrency(payment.amount)}</strong>
                    <button class="btn btn-sm btn-outline-danger" onclick="removePayment(${index})">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            </div>
        `;
    }).join('');
}

function removePayment(index) {
    payments.splice(index, 1);
    renderPayments();
    updatePaymentsSummary();
    showToast('Pagamento removido', 'info');
}

function updatePaymentsSummary() {
    const total = parseFloat(document.getElementById('totalAmount').textContent.replace('R$', '').replace('.', '').replace(',', '.'));
    const paid = payments.reduce((sum, p) => sum + p.amount, 0);
    const remaining = Math.max(0, total - paid);
    
    document.getElementById('totalPaid').textContent = formatCurrency(paid);
    document.getElementById('remaining').textContent = formatCurrency(remaining);
    
    // Habilitar botão de finalizar se totalmente pago
    const finishBtn = document.getElementById('finishSaleBtn');
    if (remaining === 0 && payments.length > 0) {
        finishBtn.disabled = false;
    } else {
        finishBtn.disabled = true;
    }
}

async function finishSale() {
    if (cart.length === 0) {
        showToast('Carrinho vazio', 'warning');
        return;
    }
    
    if (payments.length === 0) {
        showToast('Adicione pelo menos uma forma de pagamento', 'warning');
        return;
    }
    
    const remaining = getRemainingAmount();
    if (remaining > 0) {
        showToast(`Falta pagar ${formatCurrency(remaining)}`, 'warning');
        return;
    }
    
    if (!confirm('Confirma a finalização da venda?')) {
        return;
    }
    
    try {
        // 1. Criar a venda
        const saleData = {
            customerId: selectedCustomer ? selectedCustomer.id : null,
            items: cart.map(item => ({
                manipulationOrderId: item.type === 'MANIPULATION_ORDER' ? item.id : null,
                productId: item.type === 'PRODUCT' ? item.id : null,
                description: item.description,
                quantity: item.quantity,
                unitPrice: item.unitPrice,
                discountPercentage: 0
            })),
            saleDate: new Date().toISOString(),
            discountPercentage: parseFloat(document.getElementById('discountInput').value) || 0,
            observations: ''
        };
        
        const saleResponse = await fetch('/api/Sales', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(saleData)
        });
        
        const saleResult = await saleResponse.json();
        
        if (!saleResult.success) {
            throw new Error(saleResult.message || 'Erro ao criar venda');
        }
        
        const saleId = saleResult.data.id;
        
        // 2. Adicionar todos os pagamentos
        for (const payment of payments) {
            const paymentResponse = await fetch(`/api/Payments/sales/${saleId}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payment)
            });
            
            const paymentResult = await paymentResponse.json();
            
            if (!paymentResult.success) {
                throw new Error(`Erro ao registrar pagamento: ${paymentResult.message}`);
            }
        }
        
        // Sucesso!
        showToast('Venda finalizada com sucesso!', 'success');
        
        // Perguntar se quer imprimir
        if (confirm('Deseja imprimir o cupom?')) {
            printReceipt(saleId);
        }
        
        // Limpar PDV
        resetPDV();
        
    } catch (error) {
        console.error('Erro ao finalizar venda:', error);
        showToast('Erro ao finalizar venda: ' + error.message, 'error');
    }
}

function resetPDV() {
    cart = [];
    payments = [];
    selectedCustomer = null;
    
    renderCart();
    renderPayments();
    calculateTotal();
    
    document.getElementById('customerSearch').value = '';
    document.getElementById('selectedCustomerId').value = '';
    document.getElementById('discountInput').value = '0';
    document.getElementById('productSearch').focus();
}

async function loadCashRegisterInfo() {
    try {
        const response = await fetch('/api/CashRegister/current');
        const result = await response.json();
        
        if (result.success && result.data) {
            const info = result.data;
            document.getElementById('cashRegisterInfo').textContent = 
                `Caixa: ${info.code} - Aberto às ${new Date(info.openingDate).toLocaleTimeString('pt-BR')}`;
        } else {
            document.getElementById('cashRegisterInfo').textContent = 'Nenhum caixa aberto';
        }
    } catch (error) {
        console.error('Erro ao carregar info do caixa:', error);
    }
}

async function searchCustomer() {
    const modal = new bootstrap.Modal(document.getElementById('customerModal'));
    modal.show();
    
    const searchInput = document.getElementById('customerModalSearch');
    searchInput.addEventListener('input', async function() {
        if (this.value.length >= 3) {
            await loadCustomers(this.value);
        }
    });
    
    await loadCustomers('');
}

async function loadCustomers(query) {
    try {
        const response = await fetch(`/api/Customers?search=${encodeURIComponent(query)}`);
        const result = await response.json();
        
        const container = document.getElementById('customersList');
        
        if (result.success && result.data.length > 0) {
            container.innerHTML = result.data.map(customer => `
                <div class="card mb-2 cursor-pointer" onclick="selectCustomer(${JSON.stringify(customer).replace(/"/g, '&quot;')})">
                    <div class="card-body p-2">
                        <strong>${customer.fullName}</strong><br>
                        <small class="text-muted">${customer.cpf || ''} - ${customer.phone || ''}</small>
                    </div>
                </div>
            `).join('');
        } else {
            container.innerHTML = '<p class="text-muted text-center">Nenhum cliente encontrado</p>';
        }
    } catch (error) {
        console.error('Erro ao buscar clientes:', error);
    }
}

function selectCustomer(customer) {
    selectedCustomer = customer;
    document.getElementById('customerSearch').value = customer.fullName;
    document.getElementById('selectedCustomerId').value = customer.id;
    bootstrap.Modal.getInstance(document.getElementById('customerModal')).hide();
    showToast('Cliente selecionado', 'success');
}

function clearCustomer() {
    selectedCustomer = null;
    document.getElementById('customerSearch').value = '';
    document.getElementById('selectedCustomerId').value = '';
}

function printReceipt(saleId) {
    window.open(`/Sales/Receipt/${saleId}`, '_blank');
}

function closePDV() {
    if (confirm('Deseja realmente fechar o PDV?')) {
        window.location.href = '/';
    }
}

// Funções auxiliares
function formatCurrency(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value);
}

function formatPaymentMethod(method) {
    const methods = {
        'DINHEIRO': 'Dinheiro',
        'CARTAO_DEBITO': 'Cartão de Débito',
        'CARTAO_CREDITO': 'Cartão de Crédito',
        'PIX': 'PIX',
        'BOLETO': 'Boleto'
    };
    return methods[method] || method;
}

function showToast(message, type = 'info') {
    const toastContainer = document.getElementById('toastContainer') || createToastContainer();
    
    const bgColor = type === 'error' ? 'danger' : 
                    type === 'warning' ? 'warning' : 
                    type === 'success' ? 'success' : 'info';
    
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${bgColor} border-0`;
    toast.setAttribute('role', 'alert');
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">${message}</div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;
    
    toastContainer.appendChild(toast);
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();
    
    setTimeout(() => toast.remove(), 3000);
}

function createToastContainer() {
    const container = document.createElement('div');
    container.id = 'toastContainer';
    container.className = 'toast-container position-fixed top-0 end-0 p-3';
    container.style.zIndex = '9999';
    document.body.appendChild(container);
    return container;
}
