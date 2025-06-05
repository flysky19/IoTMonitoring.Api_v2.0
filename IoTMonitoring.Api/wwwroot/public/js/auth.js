// public/js/auth.js
// ����������������������������������������������������������������������������������������������������������������������
// �α���/�α׾ƿ� ���� ���۸� ��Ƶ� �����Դϴ�.

// �鿣�� API �⺻ URL (���� ������ API URL�� �����ؾ� �մϴ�)
const API_BASE = "/api";

// 1) �α��� �Լ�: ���̵�/��й�ȣ�� ������ ������ ��ū�� �޾ƿ��� ����
async function login(username, password, rememberMe) {
    try {
        // fetch()�� �̿��� POST ��û�� �����ϴ�.
        const response = await fetch(`${API_BASE}/auth/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password }),
        });

        if (!response.ok) {
            // HTTP ���� �ڵ尡 200~299�� �ƴϸ� ������ ó��
            throw new Error("�α��� ����: ���� ���� ����");
        }

        // �������� { token: "JWT ��ū ���ڿ�" } ���·� �����Ѵٰ� ����
        const data = await response.json();
        const token = data.token;

        // �α��� ���� ���� ����(rememberMe)�� ���� localStorage Ȥ�� sessionStorage�� ��ū ����
        if (rememberMe) {
            localStorage.setItem("authToken", token);
        } else {
            sessionStorage.setItem("authToken", token);
        }

        return true; // �α��� ����
    } catch (error) {
        console.error(error);
        return false; // �α��� ����
    }
}

// 2) �α׾ƿ� �Լ�: common.js���� ���� �Լ��� ������, auth.js������ �Բ� export�ؼ� ���Բ� �ۼ� ����
function logout() {
    localStorage.removeItem("authToken");
    sessionStorage.removeItem("authToken");
    location.href = "login.html";
}

// 3) �α��� �� ���� �� ȣ��Ǵ� �Լ� (login.html���� �� �Լ��� ����)
async function handleLoginFormSubmit(event) {
    event.preventDefault(); // �� ���� �� ���ΰ�ħ�Ǵ� �⺻ ���� ����

    // �� ��ҿ��� �Է°��� �����ɴϴ�.
    const username = document.getElementById("username").value.trim();
    const password = document.getElementById("password").value.trim();
    const rememberMe = document.getElementById("rememberMe").checked;

    // �Է°��� ��� ������ �� ����
    if (!username || !password) {
        alert("���̵�� ��й�ȣ�� ��� �Է����ּ���.");
        return;
    }

    // �α��� �õ�
    const success = await login(username, password, rememberMe);
    if (success) {
        // �α��� ���� �� dashboard.html�� �̵�
        location.href = "dashboard.html";
    } else {
        alert("�α��� ����! ���̵� �Ǵ� ��й�ȣ�� Ȯ���ϼ���.");
    }
}

// ����������������������������������������������������������������������������������������������������������������������
// �� ���Ͽ��� export�Ϸ��� ES ���� �ε��ؾ� �ϴµ�,
// ���� ���������� login.html���� ���� <script>�� �ҷ��ͼ� handleLoginFormSubmit()�� ������ �����Դϴ�.
