// public/js/common.js
// ����������������������������������������������������������������������������������������������������������������������
// ��� ���������� �������� ���� �Լ����� �� ���Ͽ� �ۼ��մϴ�.

// 1) localStorage Ȥ�� sessionStorage���� authToken(�α��� ��ū)�� �������� �Լ�
function getAuthToken() {
    // localStorage�� ����� ��ū�� ������ �װ��� ����
    // ������ sessionStorage���� ���������� ������� Ȯ��
    return localStorage.getItem("authToken") || sessionStorage.getItem("authToken");
}

// 2) �α��� ���θ� üũ�ؼ� �α��� ȭ������ �����̷�Ʈ��Ű�� �Լ�
function checkAuthentication() {
    const token = getAuthToken();
    // ���� ��ΰ� login.html�� �ƴ϶��(��, ȸ�� ���� ������ ���� ��),
    // ��ū�� ������ ������ login.html�� �̵���ŵ�ϴ�.
    if (!token && !location.pathname.endsWith("login.html")) {
        location.href = "login.html";
    }
}

// 3) �α׾ƿ� �Լ�: ��ū�� ��� �����ϰ� �α��� �������� �̵�
function logout() {
    localStorage.removeItem("authToken");
    sessionStorage.removeItem("authToken");
    location.href = "login.html";
}

// 4) DOMContentLoaded(���� �ε� �Ϸ�) �� �׻� ���� ���θ� üũ�ϰ� ����
//    login.html�� ��쿡�� ���� üũ�� ����ū ������ login���� �̵��� ������ �ѱ�Ƿ�, ���� �����̷�Ʈ ����
document.addEventListener("DOMContentLoaded", () => {
    checkAuthentication();
});

// ����������������������������������������������������������������������������������������������������������������������
