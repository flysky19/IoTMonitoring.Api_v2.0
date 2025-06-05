// public/js/common.js
// ───────────────────────────────────────────────────────────
// 모든 페이지에서 공통으로 사용될 함수들을 이 파일에 작성합니다.

// 1) localStorage 혹은 sessionStorage에서 authToken(로그인 토큰)을 가져오는 함수
function getAuthToken() {
    // localStorage에 저장된 토큰이 있으면 그것을 리턴
    // 없으면 sessionStorage에서 가져오도록 순서대로 확인
    return localStorage.getItem("authToken") || sessionStorage.getItem("authToken");
}

// 2) 로그인 여부를 체크해서 로그인 화면으로 리다이렉트시키는 함수
function checkAuthentication() {
    const token = getAuthToken();
    // 현재 경로가 login.html이 아니라면(즉, 회원 전용 페이지 접속 시),
    // 토큰이 없으면 강제로 login.html로 이동시킵니다.
    if (!token && !location.pathname.endsWith("login.html")) {
        location.href = "login.html";
    }
}

// 3) 로그아웃 함수: 토큰을 모두 삭제하고 로그인 페이지로 이동
function logout() {
    localStorage.removeItem("authToken");
    sessionStorage.removeItem("authToken");
    location.href = "login.html";
}

// 4) DOMContentLoaded(문서 로딩 완료) 시 항상 인증 여부를 체크하게 연결
//    login.html인 경우에는 인증 체크가 “토큰 없으면 login으로 이동” 로직을 넘기므로, 무한 리다이렉트 방지
document.addEventListener("DOMContentLoaded", () => {
    checkAuthentication();
});

// ───────────────────────────────────────────────────────────
