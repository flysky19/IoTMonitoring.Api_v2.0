// public/js/admin.js
// ����������������������������������������������������������������������������������������������������������������������
// ������ ������ ���� ��ũ��Ʈ�Դϴ�. ����� ��� ��ȸ, �߰�, ���� ���ø� �ۼ��߽��ϴ�.

// 1) ���� �ε� �Ϸ� �� ����
document.addEventListener("DOMContentLoaded", () => {
    // 1-1) ������ ���� Ȯ��: �������� ��ū ���ڵ� Ȥ�� API ȣ��� Ȯ���� �� �ֽ��ϴ�.
    const token = getAuthToken();
    // (getAuthToken �Լ��� common.js���� �̹� ���ǵ� �Լ��Դϴ�.)

    // 1-2) ����� ����� ȭ�鿡 ������
    loadUserList();

    // 1-3) "����� �߰�" ��ư Ŭ�� �̺�Ʈ ����
    document.getElementById("btnAddUser").addEventListener("click", () => {
        document.getElementById("addUserModal").style.display = "block";
    });

    // 1-4) ��� ���� �ݱ�(X) ��ư Ŭ�� �̺�Ʈ ����
    document.getElementById("btnCloseAddModal").addEventListener("click", () => {
        document.getElementById("addUserModal").style.display = "none";
    });

    // 1-5) �߰� �� ���� �̺�Ʈ ����
    document.getElementById("addUserForm").addEventListener("submit", async (e) => {
        e.preventDefault();
        await addUser();
    });
});

// 2) ����� ����� �鿣�� API(/api/admin/users)���� �����ͼ� ���̺� ������
async function loadUserList() {
    try {
        const response = await fetch("/api/admin/users", {
            headers: { Authorization: `Bearer ${getAuthToken()}` },
        });
        if (!response.ok) throw new Error("����� ��� ��ȸ ����");
        const users = await response.json();

        const tbody = document.querySelector("#userTable tbody");
        tbody.innerHTML = ""; // ���� ���� ����

        users.forEach((user) => {
            const tr = document.createElement("tr");

            // ����� ���̵�
            const tdId = document.createElement("td");
            tdId.innerText = user.username;
            tr.appendChild(tdId);

            // ����� �̸�
            const tdName = document.createElement("td");
            tdName.innerText = user.name;
            tr.appendChild(tdName);

            // ����� ����
            const tdRole = document.createElement("td");
            tdRole.innerText = user.role;
            tr.appendChild(tdRole);

            // ���� ��ư
            const tdAction = document.createElement("td");
            const btnDelete = document.createElement("button");
            btnDelete.innerText = "����";
            btnDelete.classList.add("button");
            btnDelete.style.backgroundColor = "#dc3545";   // ������
            btnDelete.style.marginLeft = "10px";
            btnDelete.onclick = () => deleteUser(user.username);
            tdAction.appendChild(btnDelete);
            tr.appendChild(tdAction);

            tbody.appendChild(tr);
        });
    } catch (error) {
        console.error(error);
        alert("����� ����� �ҷ����� �� �����߽��ϴ�.");
    }
}

// 3) ���ο� ����ڸ� �߰��ϴ� �Լ� (/api/admin/users POST)
async function addUser() {
    const newUsername = document.getElementById("newUsername").value.trim();
    const newName = document.getElementById("newName").value.trim();
    const newRole = document.getElementById("newRole").value;

    if (!newUsername || !newName) {
        alert("��� �ʵ带 �Է����ּ���.");
        return;
    }

    try {
        const response = await fetch("/api/admin/users", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                Authorization: `Bearer ${getAuthToken()}`,
            },
            body: JSON.stringify({
                username: newUsername,
                name: newName,
                role: newRole,
            }),
        });
        if (!response.ok) throw new Error("����� �߰� ����");

        // ���� �� ��� �ݰ� ��� ���ε�
        document.getElementById("addUserModal").style.display = "none";
        // �� �ʱ�ȭ
        document.getElementById("addUserForm").reset();
        await loadUserList();
    } catch (error) {
        console.error(error);
        alert("����� �߰� �� ������ �߻��߽��ϴ�.");
    }
}

// 4) Ư�� ����ڸ� �����ϴ� �Լ� (/api/admin/users/{username} DELETE)
async function deleteUser(username) {
    if (!confirm(`${username} ����ڸ� ���� �����Ͻðڽ��ϱ�?`)) return;

    try {
        const response = await fetch(`/api/admin/users/${username}`, {
            method: "DELETE",
            headers: { Authorization: `Bearer ${getAuthToken()}` },
        });
        if (!response.ok) throw new Error("����� ���� ����");

        // ���� ���� �� ��� ����
        await loadUserList();
    } catch (error) {
        console.error(error);
        alert("����� ���� �� ������ �߻��߽��ϴ�.");
    }
}

// ����������������������������������������������������������������������������������������������������������������������
