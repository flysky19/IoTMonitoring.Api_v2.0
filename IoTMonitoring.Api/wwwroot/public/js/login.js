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
function checkExistingAuth() {
    const token = localStorage.getItem('authToken');
    const userInfo = localStorage.getItem('userInfo');

    // 디버깅용 로그
    console.log('기존 토큰:', token ? '있음' : '없음');
    console.log('기존 userInfo:', userInfo);

    // 토큰과 userInfo가 모두 유효한 경우에만 리다이렉트
    if (token && userInfo && userInfo !== '{}') {
        try {
            const parsedUserInfo = JSON.parse(userInfo);
            if (parsedUserInfo.userId) {
                console.log('유효한 인증 정보 발견 - 대시보드로 이동');
                // 자동 리다이렉트를 원하지 않으면 이 부분을 주석처리
                // window.location.replace('/dashboard.html');

                // 또는 사용자에게 선택권 제공
                if (confirm('이미 로그인되어 있습니다. 대시보드로 이동하시겠습니까?')) {
                    window.location.replace('/dashboard.html');
                } else {
                    // 로그아웃 처리
                    localStorage.clear();
                    sessionStorage.clear();
                }
            }
        } catch (e) {
            console.error('userInfo 파싱 오류:', e);
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
        showError('사용자명과 비밀번호를 모두 입력해주세요.');
        return;
    }

    loginBtn.disabled = true;
    loginBtn.textContent = '로그인 중...';
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
                err = { message: `HTTP ${response.status} 오류` };
            }
            throw new Error(err.message || '로그인에 실패했습니다.');
        }

        const data = await response.json();
        console.log('로그인 응답:', data);

        // 토큰 저장 (일관된 키 사용)
        localStorage.setItem('authToken', data.token || data.authToken);

        if (data.refreshToken) {
            localStorage.setItem('refreshToken', data.refreshToken);
        }

        // 통합된 userInfo 객체 생성 및 저장
        const userInfo = {
            userId: data.userId,
            username: data.username,
            fullName: data.fullName || '',
            role: data.role || 'User',
            email: data.email || '',
            expiration: data.expiration
        };

        localStorage.setItem('userInfo', JSON.stringify(userInfo));

        // "아이디 저장" 처리
        if (rememberMe) {
            localStorage.setItem('savedUsername', username);
            localStorage.setItem('rememberMe', 'true');
        } else {
            localStorage.removeItem('savedUsername');
            localStorage.removeItem('rememberMe');
        }

        // 비밀번호는 저장하지 않음 (보안상 위험)
        localStorage.removeItem('savedPassword');

        loginBtn.textContent = '로그인 성공! 이동 중...';

        // 역할에 따른 리다이렉트
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
        loginBtn.textContent = '로그인';
    }
}

function showError(message) {
    const errorElement = document.getElementById('errorMessage');
    if (errorElement) {
        errorElement.textContent = message;
        errorElement.style.display = 'block';
    }
}

// 페이지 로드 시 저장된 계정 정보 불러오기 및 인증 체크
document.addEventListener('DOMContentLoaded', function () {

    console.log('Login 페이지 로드');

    // 저장된 아이디 불러오기
    const savedUsername = localStorage.getItem('savedUsername');
    const rememberMe = localStorage.getItem('rememberMe') === 'true';

    if (rememberMe && savedUsername) {
        document.getElementById('username').value = savedUsername;
        document.getElementById('rememberMe').checked = true;
    }

    // 로그인 폼에 이벤트 리스너 연결 (하나만!)
    const loginForm = document.getElementById('loginForm');
    if (loginForm) {
        loginForm.addEventListener('submit', handleLogin);
    }

    document.getElementById('username').focus();

});


// 로그인 폼 제출 처리
//document.getElementById('loginForm').addEventListener('submit', async function (e) {
//    e.preventDefault();
//    const username = document.getElementById('username').value;
//    const password = document.getElementById('password').value;
//    const rememberMe = document.getElementById('rememberMe').checked;
//    const loginBtn = document.getElementById('loginBtn');
//    const errorMessage = document.getElementById('errorMessage');

//    if (!username.trim() || !password.trim()) {
//        errorMessage.textContent = '사용자명과 비밀번호를 모두 입력해주세요.';
//        errorMessage.style.display = 'block';
//        return;
//    }

//    loginBtn.disabled = true;
//    loginBtn.textContent = '로그인 중...';
//    errorMessage.style.display = 'none';

//    try {
//        const response = await fetch('/api/auth/login', {
//            method: 'POST', headers: { 'Content-Type': 'application/json' },
//            body: JSON.stringify({ username, password })
//        });
//        if (!response.ok) {
//            let err;
//            try { err = await response.json(); } catch { err = { message: `HTTP ${response.status} 오류` }; }
//            throw new Error(err.message || '로그인에 실패했습니다.');
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
//        loginBtn.textContent = '로그인 성공! 이동 중...';

//        if (data.role === 'Admin' || data.role === 'admin') {
//            window.location.href = '/admin-management.html';
//        } else {
//            window.location.href = '/dashboard.html';
//        }

//    } catch (err) {
//        errorMessage.textContent = err.message;
//        errorMessage.style.display = 'block';
//    } finally {
//        if (loginBtn.textContent !== '로그인 성공! 이동 중...') {
//            loginBtn.disabled = false;
//            loginBtn.textContent = '로그인';
//        }
//    }
//});

// 엔터키로 폼 제출
document.getElementById('password')?.addEventListener('keypress', function (e) {
    if (e.key === 'Enter') {
        document.getElementById('loginForm').dispatchEvent(new Event('submit'));
    }
});

// 체크박스 변경 시 저장 정보 삭제
document.getElementById('rememberMe')?.addEventListener('change', function (e) {
    if (!e.target.checked) {
        localStorage.removeItem('savedUsername');
        localStorage.removeItem('rememberMe');
    }
});