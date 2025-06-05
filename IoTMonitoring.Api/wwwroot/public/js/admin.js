// public/js/admin.js
// ───────────────────────────────────────────────────────────
// 관리자 페이지 전용 스크립트입니다. 사용자 목록 조회, 추가, 삭제 예시를 작성했습니다.

// 1) 문서 로딩 완료 후 실행
document.addEventListener("DOMContentLoaded", () => {
    // 1-1) 관리자 권한 확인: 서버에서 토큰 디코딩 혹은 API 호출로 확인할 수 있습니다.
    const token = getAuthToken();
    // (getAuthToken 함수는 common.js에서 이미 정의된 함수입니다.)

    // 1-2) 사용자 목록을 화면에 렌더링
    loadUserList();

    // 1-3) "사용자 추가" 버튼 클릭 이벤트 연결
    document.getElementById("btnAddUser").addEventListener("click", () => {
        document.getElementById("addUserModal").style.display = "block";
    });

    // 1-4) 모달 내부 닫기(X) 버튼 클릭 이벤트 연결
    document.getElementById("btnCloseAddModal").addEventListener("click", () => {
        document.getElementById("addUserModal").style.display = "none";
    });

    // 1-5) 추가 폼 제출 이벤트 연결
    document.getElementById("addUserForm").addEventListener("submit", async (e) => {
        e.preventDefault();
        await addUser();
    });
});

// 2) 사용자 목록을 백엔드 API(/api/admin/users)에서 가져와서 테이블에 렌더링
async function loadUserList() {
    try {
        const response = await fetch("/api/admin/users", {
            headers: { Authorization: `Bearer ${getAuthToken()}` },
        });
        if (!response.ok) throw new Error("사용자 목록 조회 실패");
        const users = await response.json();

        const tbody = document.querySelector("#userTable tbody");
        tbody.innerHTML = ""; // 기존 내용 비우기

        users.forEach((user) => {
            const tr = document.createElement("tr");

            // 사용자 아이디
            const tdId = document.createElement("td");
            tdId.innerText = user.username;
            tr.appendChild(tdId);

            // 사용자 이름
            const tdName = document.createElement("td");
            tdName.innerText = user.name;
            tr.appendChild(tdName);

            // 사용자 역할
            const tdRole = document.createElement("td");
            tdRole.innerText = user.role;
            tr.appendChild(tdRole);

            // 삭제 버튼
            const tdAction = document.createElement("td");
            const btnDelete = document.createElement("button");
            btnDelete.innerText = "삭제";
            btnDelete.classList.add("button");
            btnDelete.style.backgroundColor = "#dc3545";   // 빨간색
            btnDelete.style.marginLeft = "10px";
            btnDelete.onclick = () => deleteUser(user.username);
            tdAction.appendChild(btnDelete);
            tr.appendChild(tdAction);

            tbody.appendChild(tr);
        });
    } catch (error) {
        console.error(error);
        alert("사용자 목록을 불러오는 데 실패했습니다.");
    }
}

// 3) 새로운 사용자를 추가하는 함수 (/api/admin/users POST)
async function addUser() {
    const newUsername = document.getElementById("newUsername").value.trim();
    const newName = document.getElementById("newName").value.trim();
    const newRole = document.getElementById("newRole").value;

    if (!newUsername || !newName) {
        alert("모든 필드를 입력해주세요.");
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
        if (!response.ok) throw new Error("사용자 추가 실패");

        // 성공 시 모달 닫고 목록 리로드
        document.getElementById("addUserModal").style.display = "none";
        // 폼 초기화
        document.getElementById("addUserForm").reset();
        await loadUserList();
    } catch (error) {
        console.error(error);
        alert("사용자 추가 중 오류가 발생했습니다.");
    }
}

// 4) 특정 사용자를 삭제하는 함수 (/api/admin/users/{username} DELETE)
async function deleteUser(username) {
    if (!confirm(`${username} 사용자를 정말 삭제하시겠습니까?`)) return;

    try {
        const response = await fetch(`/api/admin/users/${username}`, {
            method: "DELETE",
            headers: { Authorization: `Bearer ${getAuthToken()}` },
        });
        if (!response.ok) throw new Error("사용자 삭제 실패");

        // 삭제 성공 시 목록 갱신
        await loadUserList();
    } catch (error) {
        console.error(error);
        alert("사용자 삭제 중 오류가 발생했습니다.");
    }
}

// ───────────────────────────────────────────────────────────
