// public/js/auth.js
// ───────────────────────────────────────────────────────────
// 로그인/로그아웃 관련 동작만 모아둔 파일입니다.

// 백엔드 API 기본 URL (실제 본인의 API URL로 수정해야 합니다)
const API_BASE = "/api";

// 1) 로그인 함수: 아이디/비밀번호를 서버에 보내서 토큰을 받아오고 저장
async function login(username, password, rememberMe) {
    try {
        // fetch()를 이용해 POST 요청을 보냅니다.
        const response = await fetch(`${API_BASE}/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password }),
        });

        if (!response.ok) {
            // HTTP 상태 코드가 200~299가 아니면 오류로 처리
            throw new Error("로그인 실패: 서버 응답 오류");
        }

        // 서버에서 { token: "JWT 토큰 문자열" } 형태로 응답한다고 가정
        const data = await response.json();
        const token = data.token;

        // 로그인 상태 유지 여부(rememberMe)에 따라 localStorage 혹은 sessionStorage에 토큰 저장
        if (rememberMe) {
            localStorage.setItem("authToken", token);
        } else {
            sessionStorage.setItem("authToken", token);
        }

        return true; // 로그인 성공
    } catch (error) {
        console.error(error);
        return false; // 로그인 실패
    }
}

// 2) 로그아웃 함수: common.js에도 같은 함수가 있으나, auth.js에서도 함께 export해서 쓰게끔 작성 가능
function logout() {
    localStorage.removeItem("authToken");
    sessionStorage.removeItem("authToken");
    location.href = "login.html";
}

// 3) 로그인 폼 제출 시 호출되는 함수 (login.html에서 이 함수를 연결)
async function handleLoginFormSubmit(event) {
    event.preventDefault(); // 폼 제출 시 새로고침되는 기본 동작 방지

    // 폼 요소에서 입력값을 가져옵니다.
    const username = document.getElementById("username").value.trim();
    const password = document.getElementById("password").value.trim();
    const rememberMe = document.getElementById("rememberMe").checked;

    // 입력값이 비어 있으면 얼럿 띄우기
    if (!username || !password) {
        alert("아이디와 비밀번호를 모두 입력해주세요.");
        return;
    }

    // 로그인 시도
    const success = await login(username, password, rememberMe);
    if (success) {
        // 로그인 성공 시 dashboard.html로 이동
        location.href = "dashboard.html";
    } else {
        alert("로그인 실패! 아이디 또는 비밀번호를 확인하세요.");
    }
}

// ───────────────────────────────────────────────────────────
// 이 파일에서 export하려면 ES 모듈로 로드해야 하는데,
// 지금 예제에서는 login.html에서 직접 <script>로 불러와서 handleLoginFormSubmit()을 연결할 예정입니다.
