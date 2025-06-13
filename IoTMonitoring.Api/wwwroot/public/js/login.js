// ���� ��� �̹��� ����
const images = [
    './images/bg1.jpg', './images/bg2.jpg', './images/bg3.jpg', './images/bg4.jpg', './images/bg5.jpg',
    './images/bg6.jpg', './images/bg7.jpg', './images/bg8.jpg', './images/bg9.jpg', './images/bg10.jpg'
];
document.addEventListener('DOMContentLoaded', () => {
    const rand = Math.floor(Math.random() * images.length);
    document.body.style.backgroundImage = `url('${images[rand]}')`;
});

/*document.addEventListener('DOMContentLoaded', checkCompanyModalStructure);*/
function checkExistingAuth() {
    const token = localStorage.getItem('authToken');
    const userInfo = localStorage.getItem('userInfo');

    // ������ �α�
    console.log('���� ��ū:', token ? '����' : '����');
    console.log('���� userInfo:', userInfo);

    // ��ū�� userInfo�� ��� ��ȿ�� ��쿡�� �����̷�Ʈ
    if (token && userInfo && userInfo !== '{}') {
        try {
            const parsedUserInfo = JSON.parse(userInfo);
            if (parsedUserInfo.userId) {
                console.log('��ȿ�� ���� ���� �߰� - ��ú���� �̵�');
                // �ڵ� �����̷�Ʈ�� ������ ������ �� �κ��� �ּ�ó��
                // window.location.replace('/dashboard.html');

                // �Ǵ� ����ڿ��� ���ñ� ����
                if (confirm('�̹� �α��εǾ� �ֽ��ϴ�. ��ú���� �̵��Ͻðڽ��ϱ�?')) {
                    window.location.replace('/dashboard.html');
                } else {
                    // �α׾ƿ� ó��
                    localStorage.clear();
                    sessionStorage.clear();
                }
            }
        } catch (e) {
            console.error('userInfo �Ľ� ����:', e);
            localStorage.removeItem('userInfo');
        }
    }
}

async function handleLogin(e) {
    e.preventDefault();

    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const rememberMe = document.getElementById('rememberMe').checked;
    const loginBtn = document.getElementById('loginBtn');
    const errorMessage = document.getElementById('errorMessage');

    if (!username.trim() || !password.trim()) {
        showError('����ڸ�� ��й�ȣ�� ��� �Է����ּ���.');
        return;
    }

    loginBtn.disabled = true;
    loginBtn.textContent = '�α��� ��...';
    errorMessage.style.display = 'none';

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        if (!response.ok) {
            let err;
            try {
                err = await response.json();
            } catch {
                err = { message: `HTTP ${response.status} ����` };
            }
            throw new Error(err.message || '�α��ο� �����߽��ϴ�.');
        }

        const data = await response.json();
        console.log('�α��� ����:', data);

        // ��ū ���� (�ϰ��� Ű ���)
        localStorage.setItem('authToken', data.token || data.authToken);

        if (data.refreshToken) {
            localStorage.setItem('refreshToken', data.refreshToken);
        }

        // ���յ� userInfo ��ü ���� �� ����
        const userInfo = {
            userId: data.userId,
            username: data.username,
            fullName: data.fullName || '',
            role: data.role || 'User',
            email: data.email || '',
            expiration: data.expiration
        };

        localStorage.setItem('userInfo', JSON.stringify(userInfo));

        // "���̵� ����" ó��
        if (rememberMe) {
            localStorage.setItem('savedUsername', username);
            localStorage.setItem('rememberMe', 'true');
        } else {
            localStorage.removeItem('savedUsername');
            localStorage.removeItem('rememberMe');
        }

        // ��й�ȣ�� �������� ���� (���Ȼ� ����)
        localStorage.removeItem('savedPassword');

        loginBtn.textContent = '�α��� ����! �̵� ��...';

        // ���ҿ� ���� �����̷�Ʈ
        setTimeout(() => {
            if (data.role === 'Admin' || data.role === 'admin') {
                window.location.href = '/admin-management.html';
            } else {
                window.location.href = '/dashboard.html';
            }
        }, 500);

    } catch (err) {
        showError(err.message);
        loginBtn.disabled = false;
        loginBtn.textContent = '�α���';
    }
}

function showError(message) {
    const errorElement = document.getElementById('errorMessage');
    if (errorElement) {
        errorElement.textContent = message;
        errorElement.style.display = 'block';
    }
}

// ������ �ε� �� ����� ���� ���� �ҷ����� �� ���� üũ
document.addEventListener('DOMContentLoaded', function () {

    console.log('Login ������ �ε�');

    // ����� ���̵� �ҷ�����
    const savedUsername = localStorage.getItem('savedUsername');
    const rememberMe = localStorage.getItem('rememberMe') === 'true';

    if (rememberMe && savedUsername) {
        document.getElementById('username').value = savedUsername;
        document.getElementById('rememberMe').checked = true;
    }

    // �α��� ���� �̺�Ʈ ������ ���� (�ϳ���!)
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', handleLogin);
    }

    document.getElementById('username').focus();

});


// �α��� �� ���� ó��
//document.getElementById('loginForm').addEventListener('submit', async function (e) {
//    e.preventDefault();
//    const username = document.getElementById('username').value;
//    const password = document.getElementById('password').value;
//    const rememberMe = document.getElementById('rememberMe').checked;
//    const loginBtn = document.getElementById('loginBtn');
//    const errorMessage = document.getElementById('errorMessage');

//    if (!username.trim() || !password.trim()) {
//        errorMessage.textContent = '����ڸ�� ��й�ȣ�� ��� �Է����ּ���.';
//        errorMessage.style.display = 'block';
//        return;
//    }

//    loginBtn.disabled = true;
//    loginBtn.textContent = '�α��� ��...';
//    errorMessage.style.display = 'none';

//    try {
//        const response = await fetch('/api/auth/login', {
//            method: 'POST', headers: { 'Content-Type': 'application/json' },
//            body: JSON.stringify({ username, password })
//        });
//        if (!response.ok) {
//            let err;
//            try { err = await response.json(); } catch { err = { message: `HTTP ${response.status} ����` }; }
//            throw new Error(err.message || '�α��ο� �����߽��ϴ�.');
//        }
//        const data = await response.json();
//        localStorage.setItem('authToken', data.token);
//        localStorage.setItem('refreshToken', data.refreshToken);
//        localStorage.setItem('userId', data.userId);
//        localStorage.setItem('username', data.username);
//        localStorage.setItem('fullName', data.fullName);
//        localStorage.setItem('expiration', data.expiration);
//        localStorage.setItem('lastLogin', data.lastLogin);
//        if (rememberMe) {
//            localStorage.setItem('savedUsername', username);
//            localStorage.setItem('savedPassword', password);
//            localStorage.setItem('rememberMe', 'true');
//        } else {
//            localStorage.removeItem('savedUsername');
//            localStorage.removeItem('savedPassword');
//            localStorage.removeItem('rememberMe');
//        }
//        loginBtn.textContent = '�α��� ����! �̵� ��...';

//        if (data.role === 'Admin' || data.role === 'admin') {
//            window.location.href = '/admin-management.html';
//        } else {
//            window.location.href = '/dashboard.html';
//        }

//    } catch (err) {
//        errorMessage.textContent = err.message;
//        errorMessage.style.display = 'block';
//    } finally {
//        if (loginBtn.textContent !== '�α��� ����! �̵� ��...') {
//            loginBtn.disabled = false;
//            loginBtn.textContent = '�α���';
//        }
//    }
//});

// ����Ű�� �� ����
document.getElementById('password')?.addEventListener('keypress', function (e) {
    if (e.key === 'Enter') {
        document.getElementById('loginForm').dispatchEvent(new Event('submit'));
    }
});

// üũ�ڽ� ���� �� ���� ���� ����
document.getElementById('rememberMe')?.addEventListener('change', function (e) {
    if (!e.target.checked) {
        localStorage.removeItem('savedUsername');
        localStorage.removeItem('rememberMe');
    }
});