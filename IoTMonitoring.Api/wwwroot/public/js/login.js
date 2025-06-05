// 랜덤 배경 이미지 설정
const images = [
    './images/bg1.jpg', './images/bg2.jpg', './images/bg3.jpg', './images/bg4.jpg', './images/bg5.jpg',
    './images/bg6.jpg', './images/bg7.jpg', './images/bg8.jpg', './images/bg9.jpg', './images/bg10.jpg'
];
document.addEventListener('DOMContentLoaded', () => {
    const rand = Math.floor(Math.random() * images.length);
    document.body.style.backgroundImage = `url('${images[rand]}')`;
});

/*document.addEventListener('DOMContentLoaded', checkCompanyModalStructure);*/

// 페이지 로드 시 저장된 계정 정보 불러오기 및 인증 체크
document.addEventListener('DOMContentLoaded', function () {
    const savedUsername = localStorage.getItem('savedUsername');
    const savedPassword = localStorage.getItem('savedPassword');
    const rememberMe = localStorage.getItem('rememberMe') === 'true';
    if (rememberMe && savedUsername) {
        document.getElementById('username').value = savedUsername;
        document.getElementById('rememberMe').checked = true;
        if (savedPassword) document.getElementById('password').value = savedPassword;
    }
    if (localStorage.getItem('authToken')) {
        window.location.href = '/dashboard.html';
    }
    document.getElementById('username').focus();
});

// 로그인 폼 제출 처리
document.getElementById('loginForm').addEventListener('submit', async function (e) {
    e.preventDefault();
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const rememberMe = document.getElementById('rememberMe').checked;
    const loginBtn = document.getElementById('loginBtn');
    const errorMessage = document.getElementById('errorMessage');

    if (!username.trim() || !password.trim()) {
        errorMessage.textContent = '사용자명과 비밀번호를 모두 입력해주세요.';
        errorMessage.style.display = 'block';
        return;
    }

    loginBtn.disabled = true;
    loginBtn.textContent = '로그인 중...';
    errorMessage.style.display = 'none';

    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST', headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });
        if (!response.ok) {
            let err;
            try { err = await response.json(); } catch { err = { message: `HTTP ${response.status} 오류` }; }
            throw new Error(err.message || '로그인에 실패했습니다.');
        }
        const data = await response.json();
        localStorage.setItem('authToken', data.token);
        localStorage.setItem('refreshToken', data.refreshToken);
        localStorage.setItem('userId', data.userId);
        localStorage.setItem('username', data.username);
        localStorage.setItem('fullName', data.fullName);
        localStorage.setItem('expiration', data.expiration);
        localStorage.setItem('lastLogin', data.lastLogin);
        if (rememberMe) {
            localStorage.setItem('savedUsername', username);
            localStorage.setItem('savedPassword', password);
            localStorage.setItem('rememberMe', 'true');
        } else {
            localStorage.removeItem('savedUsername');
            localStorage.removeItem('savedPassword');
            localStorage.removeItem('rememberMe');
        }
        loginBtn.textContent = '로그인 성공! 이동 중...';
        setTimeout(() => window.location.href = '/dashboard.html', 500);
    } catch (err) {
        errorMessage.textContent = err.message;
        errorMessage.style.display = 'block';
    } finally {
        if (loginBtn.textContent !== '로그인 성공! 이동 중...') {
            loginBtn.disabled = false;
            loginBtn.textContent = '로그인';
        }
    }
});

// 엔터키로 폼 제출
document.getElementById('password').addEventListener('keypress', function (e) {
    if (e.key === 'Enter') document.getElementById('loginForm').dispatchEvent(new Event('submit'));
});

// 체크박스 변경 시 저장 정보 삭제
document.getElementById('rememberMe').addEventListener('change', function (e) {
    if (!e.target.checked) {
        localStorage.removeItem('savedUsername');
        localStorage.removeItem('savedPassword');
        localStorage.removeItem('rememberMe');
    }
});