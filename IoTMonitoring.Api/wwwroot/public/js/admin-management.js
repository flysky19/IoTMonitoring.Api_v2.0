
let sensors = [];
let users = [];
let companies = [];
let sensorGroups = [];
let currentTab = 'sensors';
let currentEditingSensorId = null;

// === 센서 그룹 멤버 관리 함수들 ===
let currentGroupId = null;
let allSensors = [];
let groupSensors = [];
let selectedChanges = new Map(); // 변경사항 추적

// 페이지 로드 시 초기화
document.addEventListener('DOMContentLoaded', () => {

    // URL 해시에 따라 탭 선택
    const hash = window.location.hash.substring(1); // #제거
    console.log(`센서 ${hash} hash`);


    switch (hash) {
        case 'users':
            showTab('users');
            break;
        case 'companies':
            showTab('companies');
            break;
        case 'sensor-groups':
            showTab('sensor-groups');
            break;
        case 'sensors':
        default:
            showTab('sensors');
            break;
    }

    //const companyForm = document.getElementById('companyForm');
    //if (companyForm) {
    //    console.log('회사 폼 찾음!'); // 디버깅용
    //    companyForm.addEventListener('submit', async (e) => {
    //        e.preventDefault();
    //        console.log('회사 폼 제출됨!'); // 디버깅용

    //    });
    //} else {
    //    console.error('companyForm을 찾을 수 없습니다!');
    //}

});

// URL 해시 변경 감지
window.addEventListener('hashchange', function () {
    const hash = window.location.hash.substring(1);
    if (hash && ['users', 'companies', 'sensors', 'sensor-groups'].includes(hash)) {
        showTab(hash);
    }
});

// 인증 토큰 가져오기
function getAuthToken() {
    return localStorage.getItem('authToken');
}

// 대시보드로 돌아가기
function goBack() {
    window.location.href = '/dashboard.html';
}

// 브라우저 콘솔에서 실행
function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));
        return JSON.parse(jsonPayload);
    } catch (e) {
        return null;
    }
}

// 센서 그룹 멤버 편집 모달 열기
async function editGroupMembers(groupId) {
    currentGroupId = groupId;
    selectedChanges.clear();

    // 그룹 정보 가져오기
    const group = sensorGroups.find(g => g.groupID === groupId);
    if (!group) {
        alert('그룹 정보를 찾을 수 없습니다.');
        return;
    }

    document.getElementById('sensorGroupMemberTitle').textContent = `${group.groupName} - 센서 멤버 관리`;

    // 모든 센서와 그룹 센서 로드
    await loadAllSensorsForGroup();
    await loadGroupSensors(groupId);

    // 리스트 렌더링
    renderSensorLists();

    // 모달 표시
    document.getElementById('sensorGroupMemberModal').style.display = 'block';
}
// 로딩 오버레이 표시/숨김 함수
function showLoadingOverlay(message) {
    const overlay = document.createElement('div');
    overlay.id = 'loadingOverlay';
    overlay.className = 'loading-overlay';
    overlay.innerHTML = `
            <div class="loading-content">
                <div class="loading-spinner"></div>
                <div class="loading-message">${message}</div>
            </div>
        `;
    document.body.appendChild(overlay);
}

function hideLoadingOverlay() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.remove();
    }
}

// 센서 리스트 렌더링
function renderSensorLists() {
    const includedContainer = document.getElementById('includedSensorsList');
    const availableContainer = document.getElementById('availableSensorsList');

    // 현재 그룹에 포함된 센서 ID 목록
    const groupSensorIds = new Set(groupSensors.map(s => s.sensorID));

    // selectedChanges 반영
    selectedChanges.forEach((action, sensorId) => {
        if (action === 'add') {
            groupSensorIds.add(sensorId);
        } else if (action === 'remove') {
            groupSensorIds.delete(sensorId);
        }
    });

    // 포함된 센서와 미포함 센서 분리
    const includedSensors = allSensors.filter(s => groupSensorIds.has(s.sensorID));
    const availableSensors = allSensors.filter(s => !groupSensorIds.has(s.sensorID));

    // 카운트 업데이트
    document.getElementById('includedCount').textContent = `(${includedSensors.length})`;
    document.getElementById('availableCount').textContent = `(${availableSensors.length})`;

    // 포함된 센서 렌더링
    if (includedSensors.length === 0) {
        includedContainer.innerHTML = '<div class="empty-state">그룹에 포함된 센서가 없습니다.</div>';
    } else {
        includedContainer.innerHTML = includedSensors.map(sensor =>
            createSensorMemberItem(sensor, true)
        ).join('');
    }

    // 사용 가능한 센서 렌더링
    if (availableSensors.length === 0) {
        availableContainer.innerHTML = '<div class="empty-state">추가 가능한 센서가 없습니다.</div>';
    } else {
        availableContainer.innerHTML = availableSensors.map(sensor =>
            createSensorMemberItem(sensor, false)
        ).join('');
    }
}

// 센서 아이템 HTML 생성
function createSensorMemberItem(sensor, isIncluded) {
    const sensorTypeInfo = {
        'particle': { icon: '💨', name: '미세먼지' },
        'temp_humidity': { icon: '🌡️', name: '온습도' },
        'wind': { icon: '🌪️', name: '풍속' },
        'speaker': { icon: '🔊', name: '스피커' }
    };

    const typeInfo = sensorTypeInfo[sensor.sensorType] || { icon: '📡', name: '기타' };
    const isOnline = sensor.connectionStatus === 'online';

    return `
            <div class="sensor-member-item" data-sensor-id="${sensor.sensorID}">
                <input type="checkbox"
                       class="sensor-checkbox"
                       onchange="toggleSensorSelection(${sensor.sensorID}, ${isIncluded})"
                       ${isMarkedForChange(sensor.sensorID, isIncluded) ? 'checked' : ''}>
                <div class="sensor-member-info">
                    <div class="sensor-member-details">
                        <div class="sensor-member-name">
                            ${typeInfo.icon} ${sensor.name || sensor.sensorUUID}
                        </div>
                        <div class="sensor-member-meta">
                            <span>타입: ${typeInfo.name}</span>
                            <span>UUID: ${sensor.sensorUUID}</span>
                        </div>
                    </div>
                    <div class="sensor-member-actions">
                        <div class="sensor-member-status">
                            <span class="status-indicator ${isOnline ? 'online' : 'offline'}"></span>
                            <span>${isOnline ? '온라인' : '오프라인'}</span>
                        </div>
                        ${isIncluded ? `
                            <button class="remove-btn" onclick="removeSensorFromGroup(${sensor.sensorID})" title="그룹에서 제거">
                                ❌
                            </button>
                        ` : `
                            <button class="add-btn" onclick="addSensorToGroup(${sensor.sensorID})" title="그룹에 추가">
                                ➕
                            </button>
                        `}
                    </div>
                </div>
            </div>
        `;
}

// 센서를 그룹에서 즉시 제거
function removeSensorFromGroup(sensorId) {
    if (!confirm('이 센서를 그룹에서 제거하시겠습니까?')) {
        return;
    }

    // 변경사항에 추가
    selectedChanges.set(sensorId, 'remove');

    // 애니메이션 효과와 함께 UI 업데이트
    const item = document.querySelector(`[data-sensor-id="${sensorId}"]`);
    if (item) {
        item.classList.add('removing');
        setTimeout(() => {
            renderSensorLists();
        }, 300);
    }
}

// 센서를 그룹에 즉시 추가
function addSensorToGroup(sensorId) {
    // 변경사항에 추가
    selectedChanges.set(sensorId, 'add');

    // 애니메이션 효과와 함께 UI 업데이트
    const item = document.querySelector(`[data-sensor-id="${sensorId}"]`);
    if (item) {
        item.classList.add('adding');
        setTimeout(() => {
            renderSensorLists();
        }, 300);
    }
}
// 전체 선택/해제 기능 추가
function toggleAllSensors(isIncluded) {
    const checkboxes = document.querySelectorAll(
        isIncluded ? '#includedSensorsList .sensor-checkbox' : '#availableSensorsList .sensor-checkbox'
    );

    const allChecked = Array.from(checkboxes).every(cb => cb.checked);

    checkboxes.forEach(checkbox => {
        checkbox.checked = !allChecked;
        const sensorId = parseInt(checkbox.closest('[data-sensor-id]').dataset.sensorId);

        if (!allChecked) {
            selectedChanges.set(sensorId, isIncluded ? 'remove' : 'add');
        } else {
            selectedChanges.delete(sensorId);
        }
    });

    // UI 업데이트
    document.querySelectorAll('.sensor-member-item').forEach(item => {
        const checkbox = item.querySelector('.sensor-checkbox');
        if (checkbox.checked) {
            item.classList.add('selected');
        } else {
            item.classList.remove('selected');
        }
    });
}

// 변경 표시 확인
function isMarkedForChange(sensorId, isIncluded) {
    const change = selectedChanges.get(sensorId);
    if (isIncluded) {
        return change === 'remove';
    } else {
        return change === 'add';
    }
}

// 센서 선택 토글
function toggleSensorSelection(sensorId, isCurrentlyIncluded) {
    const item = document.querySelector(`[data-sensor-id="${sensorId}"]`);

    if (isCurrentlyIncluded) {
        // 현재 포함된 센서를 체크하면 제거 예정
        if (selectedChanges.get(sensorId) === 'remove') {
            selectedChanges.delete(sensorId);
            item.classList.remove('selected');
        } else {
            selectedChanges.set(sensorId, 'remove');
            item.classList.add('selected');
        }
    } else {
        // 미포함 센서를 체크하면 추가 예정
        if (selectedChanges.get(sensorId) === 'add') {
            selectedChanges.delete(sensorId);
            item.classList.remove('selected');
        } else {
            selectedChanges.set(sensorId, 'add');
            item.classList.add('selected');
        }
    }

    // 애니메이션 효과
    item.classList.add('moving');
    setTimeout(() => item.classList.remove('moving'), 300);
}

// 포함된 센서 필터링
function filterIncludedSensors() {
    const searchTerm = document.getElementById('includedSearchInput').value.toLowerCase();
    const items = document.querySelectorAll('#includedSensorsList .sensor-member-item');

    items.forEach(item => {
        const text = item.textContent.toLowerCase();
        item.style.display = text.includes(searchTerm) ? 'flex' : 'none';
    });
}

// 사용 가능한 센서 필터링
function filterAvailableSensors() {
    const searchTerm = document.getElementById('availableSearchInput').value.toLowerCase();
    const items = document.querySelectorAll('#availableSensorsList .sensor-member-item');

    items.forEach(item => {
        const text = item.textContent.toLowerCase();
        item.style.display = text.includes(searchTerm) ? 'flex' : 'none';
    });
}

// 센서 그룹 멤버 저장
async function saveSensorGroupMembers() {
    if (selectedChanges.size === 0) {
        alert('변경사항이 없습니다.');
        return;
    }

    const addSensors = [];
    const removeSensors = [];

    selectedChanges.forEach((action, sensorId) => {
        if (action === 'add') {
            addSensors.push(sensorId);
        } else if (action === 'remove') {
            removeSensors.push(sensorId);
        }
    });

    const confirmMessage = `다음 변경사항을 적용하시겠습니까?\n\n` +
        `• 추가할 센서: ${addSensors.length}개\n` +
        `• 제거할 센서: ${removeSensors.length}개`;

    if (!confirm(confirmMessage)) {
        return;
    }

    let successCount = 0;
    let failedSensors = [];

    try {

        showLoadingOverlay('센서 그룹 멤버를 업데이트하는 중...');

        const totalChanges = addSensors.length + removeSensors.length;

        // 센서 추가
        // 센서 추가
        for (const sensorId of addSensors) {
            try {
                await updateSensorGroup(sensorId, currentGroupId);
                successCount++;
            } catch (error) {
                failedSensors.push({ id: sensorId, action: '추가', error: error.message });
            }
        }

        // 센서 제거 (그룹 ID를 null로 설정)
        for (const sensorId of removeSensors) {
            try {
                await updateSensorGroup(sensorId, null);
                successCount++;
            } catch (error) {
                failedSensors.push({ id: sensorId, action: '제거', error: error.message });
            }
        }

        // 결과 메시지 생성
        let message = `총 ${totalChanges}개 중 ${successCount}개 센서 변경 완료`;

        if (failedSensors.length > 0) {
            message += `\n\n실패한 센서 (${failedSensors.length}개):`;
            failedSensors.forEach(failed => {
                message += `\n- 센서 ${failed.id} ${failed.action} 실패`;
            });
            message += '\n\n실패한 센서는 다시 시도해주세요.';
        }

        alert(message);

        // 성공한 변경사항만 있으면 모달 닫기
        if (successCount > 0) {
            closeSensorGroupMemberModal();
            await loadSensorGroups();
            await loadSensors();
        }

        // 모달 닫고 그룹 목록 새로고침
        closeSensorGroupMemberModal();
        loadSensorGroups();

    } catch (error) {
        alert('센서 그룹 멤버 수정 중 오류가 발생했습니다: ' + error.message);
    }
}

// 센서의 그룹 업데이트
async function updateSensorGroup(sensorId, groupId) {
    const sensor = allSensors.find(s => s.sensorID === sensorId);
    if (!sensor) return;

    const updateData = {
        GroupID: groupId,
    };

    const response = await fetch(`/api/sensors/${sensorId}/group`, {
        method: 'PATCH',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${getAuthToken()}`
        },
        body: JSON.stringify(updateData)
    });

    if (!response.ok) {
        const errorText = await response.text();
        console.error(`센서 ${sensorId} 그룹 변경 실패:`, errorText);
        throw new Error(`센서 ${sensorId} 그룹 변경 실패: ${errorText}`);
    }

    const result = await response.json();
    console.log(`센서 ${sensorId} 그룹 변경 성공:`, result);
    return result;

}

// 모달 닫기
function closeSensorGroupMemberModal() {
    document.getElementById('sensorGroupMemberModal').style.display = 'none';
    currentGroupId = null;
    selectedChanges.clear();
    document.getElementById('includedSearchInput').value = '';
    document.getElementById('availableSearchInput').value = '';
}

// 모든 센서 로드
async function loadAllSensorsForGroup() {
    try {
        const response = await fetch('/api/sensors', {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            allSensors = await response.json();
        }
    } catch (error) {
        console.error('센서 로드 실패:', error);
    }
}

// 그룹 센서 로드
async function loadGroupSensors(groupId) {
    try {
        const response = await fetch(`/api/sensor-groups/${groupId}/sensors`, {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            groupSensors = await response.json();
        }
    } catch (error) {
        console.error('그룹 센서 로드 실패:', error);
    }
}


// 탭 전환
function showTab(tabName, evt) {
    // 1) 모든 탭(tab)과 탭 콘텐츠(tab-content)에서 active 클래스 제거
    document.querySelectorAll('.tab').forEach(tab => tab.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(content => content.classList.remove('active'));

    console.log(`현재 탭1: ${tabName}`);

    // 2) 클릭 이벤트가 전달된 경우: evt.target.closest('.tab')를 통해 실제 버튼 요소를 찾아서 active 클래스 추가
    if (evt && evt.target) {
        const clickedTab = evt.target.closest('.tab');
        if (clickedTab) {
            clickedTab.classList.add('active');
        }
    } else {
        // 3) 초기 로드 등 이벤트가 없을 때: tabName 문자열로 적절한 탭 요소를 찾아서 active 클래스 추가
        const tabs = document.querySelectorAll('.tab');
        tabs.forEach(tab => {
            const tabText = tab.textContent.trim().toLowerCase();
            if ((tabName === 'sensors' && tabText.includes('센서') && !tabText.includes('그룹')) ||
                (tabName === 'sensor-groups' && tabText.includes('센서 그룹')) ||
                (tabName === 'users' && tabText.includes('사용자')) ||
                (tabName === 'companies' && tabText.includes('회사'))) {
                tab.classList.add('active');
            }
        });
    }

    // 4) 해당 탭 콘텐츠 활성화 (id="[tabName]-tab"이어야 함)
    const tabContent = document.getElementById(`${tabName}-tab`);
    if (tabContent) {
        tabContent.classList.add('active');
    }

    // 5) 현재 탭 변수 업데이트
    currentTab = tabName;

    console.log(`현재 탭3: ${currentTab}`);

    // 6) 탭에 따라 데이터 로드 호출
    if (tabName === 'sensors') {
        loadSensors();
    } else if (tabName === 'sensor-groups') {
        loadSensorGroups();
        loadCompanies(); // 센서 그룹 필터에 필요한 회사 데이터 로드
    } else if (tabName === 'users') {
        loadUsers();
        loadCompanies(); // 사용자 관리에서 회사 목록 필요 시
    } else if (tabName === 'companies') {
        loadCompanies();
    }
}

// === 센서 관리 함수들 ===
async function loadSensors() {
    try {
        const response = await fetch('/api/sensors', {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            sensors = await response.json();
            updateSensorStats();
            renderSensors();
        }
    } catch (error) {
        console.error('센서 로드 실패:', error);
    }
}

function updateSensorStats() {
    document.getElementById('totalSensors').textContent = sensors.length;
    document.getElementById('activeSensors').textContent = sensors.filter(s => s.status === 'active').length;
    document.getElementById('onlineSensors').textContent = sensors.filter(s => s.connectionStatus === 'online').length;
    document.getElementById('offlineSensors').textContent = sensors.filter(s => s.connectionStatus === 'offline').length;
}

function renderSensors() {
    const tbody = document.getElementById('sensorTableBody');
    const searchTerm = document.getElementById('sensorSearchInput').value.toLowerCase();

    const filtered = sensors.filter(sensor =>
        sensor.name?.toLowerCase().includes(searchTerm) ||
        sensor.sensorUUID?.toLowerCase().includes(searchTerm)
    );

    document.getElementById('sensorCount').textContent = `총 ${filtered.length}개의 센서`;

    if (filtered.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="empty-state">센서가 없습니다.</td></tr>';
        return;
    }

    tbody.innerHTML = filtered.map(sensor => `
                            <tr>
                                <td>${sensor.sensorID}</td>
                                <td>${sensor.name || '-'}</td>
                                <td><span class="badge">${sensor.sensorType}</span></td>
                                <td><span class="badge ${sensor.status}">${sensor.status}</span></td>
                                <td><span class="badge ${sensor.connectionStatus}">${sensor.connectionStatus}</span></td>
                                <td>${sensor.groupName || '-'}</td>
                                <td>${sensor.lastCommunication ? new Date(sensor.lastCommunication).toLocaleString('ko-KR') : '-'}</td>
                                <td>
                                    <div class="actions">
                                        <button class="btn btn-sm" onclick="openEditSensorModal(${sensor.sensorID})">수정</button>
                                        <button class="btn btn-sm danger" onclick="deleteSensor(${sensor.sensorID})">삭제</button>
                                    </div>
                                </td>
                            </tr>
                        `).join('');
}

function searchSensors() {
    renderSensors();
}

async function deleteSensor(sensorId) {
    if (!confirm('정말로 이 센서를 삭제하시겠습니까?')) return;

    try {
        const response = await fetch(`/api/sensors/${sensorId}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            alert('센서가 삭제되었습니다.');
            loadSensors();
        }
    } catch (error) {
        alert('센서 삭제 실패: ' + error.message);
    }
}

async function openAddSensorModal() {
    document.getElementById('addSensorForm').reset();
    document.getElementById('newInstallationDate').value = new Date().toISOString().split('T')[0];
    loadSensorGroupsForAdd();
    generateNewUUID();
    document.getElementById('addSensorModal').style.display = 'block';
}

function closeAddSensorModal() {
    document.getElementById('addSensorModal').style.display = 'none';
    document.getElementById('addSensorForm').reset();
}

function updateSensorTypeHint() {
    const type = document.getElementById('newSensorType').value;
    const hintDiv = document.getElementById('sensorTypeHint');

    const hints = {
        'temp_humidity': '💡 온도와 습도를 동시에 측정합니다. 권장 모델: DHT22, BME280',
        'particle': '💡 미세먼지 농도를 측정합니다. 권장 모델: PMS7003, SDS011',
        'wind': '💡 풍속을 측정합니다. 권장 모델: WS-2401',
        'speaker': '💡 음성 안내 및 알림용 스피커입니다.'
    };

    if (type && hints[type]) {
        hintDiv.textContent = hints[type];
        hintDiv.style.display = 'block';
    } else {
        hintDiv.style.display = 'none';
    }
}

async function loadSensorGroupsForAdd() {
    try {
        const response = await fetch('/api/sensor-groups', {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            const groups = await response.json();
            const select = document.getElementById('newSensorGroup');

            select.innerHTML = '<option value="">선택하세요</option>';
            groups.forEach(group => {
                const option = new Option(
                    `${group.groupName} (${group.companyName})`,
                    group.groupID
                );
                select.add(option);
            });
        }
    } catch (error) {
        console.error('센서 그룹 로드 실패:', error);
    }
}

// === 센서 수정 모달 함수들 ===
async function openEditSensorModal(sensorId) {
    try {

        currentEditingSensorId = sensorId;

        const response = await fetch(`/api/sensors/${sensorId}`, {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            const sensor = await response.json();

            // Hidden 필드
            document.getElementById('editSensorId').value = sensor.sensorID;

            // 읽기 전용 정보
            document.getElementById('editSensorUUID').textContent = sensor.sensorUUID;
            const typeInfo = getSensorTypeInfo(sensor.sensorType);
            document.getElementById('editSensorTypeDisplay').textContent =
                `${typeInfo.icon} ${typeInfo.name}`;
            document.getElementById('editSensorCreatedAt').textContent =
                new Date(sensor.createdAt).toLocaleDateString('ko-KR');

            // 수정 가능한 필드
            document.getElementById('editSensorName').value = sensor.name || '';
            document.getElementById('editSensorModel').value = sensor.model || '';
            document.getElementById('editHeartbeatInterval').value = sensor.heartbeatInterval || 60;
            document.getElementById('editConnectionTimeout').value = sensor.connectionTimeout || 180;
            document.getElementById('editFirmwareVersion').value = sensor.firmwareVersion || '';

            // 상태 정보
            updateSensorStatusDisplay(sensor);

            // 센서 그룹 로드 및 선택
            await loadSensorGroupsForEdit();
            if (sensor.groupID) {
                document.getElementById('editSensorGroup').value = sensor.groupID;
            }

            // 활성화/비활성화 버튼 표시 설정
            updateActivationButtons(sensor.status);

            document.getElementById('editSensorModal').style.display = 'block';
        }
    } catch (error) {
        alert('센서 정보 로드 실패: ' + error.message);
    }
}

function closeEditSensorModal() {
    document.getElementById('editSensorModal').style.display = 'none';
    document.getElementById('editSensorForm').reset();
    currentEditingSensorId = null;
}

function updateSensorStatusDisplay(sensor) {
    // 센서 상태
    const statusEl = document.getElementById('editSensorStatus');
    statusEl.textContent = sensor.status === 'active' ? '활성' : '비활성';
    statusEl.className = `badge ${sensor.status}`;

    // 연결 상태
    const connStatusEl = document.getElementById('editConnectionStatus');
    connStatusEl.textContent =
        sensor.connectionStatus === 'online' ? '온라인' :
            sensor.connectionStatus === 'offline' ? '오프라인' : '알 수 없음';
    connStatusEl.className = `badge ${sensor.connectionStatus}`;

    // 마지막 통신
    document.getElementById('editLastCommunication').textContent =
        sensor.lastCommunication ?
            new Date(sensor.lastCommunication).toLocaleString('ko-KR') : '-';

    // 마지막 하트비트
    document.getElementById('editLastHeartbeat').textContent =
        sensor.lastHeartbeat ?
            new Date(sensor.lastHeartbeat).toLocaleString('ko-KR') : '-';
}

// 활성화/비활성화 버튼 표시 업데이트 함수
function updateActivationButtons(status) {
    const activateBtn = document.getElementById('activateBtn');
    const deactivateBtn = document.getElementById('deactivateBtn');

    if (status === 'active') {
        activateBtn.style.display = 'none';
        deactivateBtn.style.display = 'flex';
    } else {
        activateBtn.style.display = 'flex';
        deactivateBtn.style.display = 'none';
    }
}

async function loadSensorGroupsForEdit() {
    try {
        const response = await fetch('/api/sensor-groups', {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            const groups = await response.json();
            const select = document.getElementById('editSensorGroup');

            select.innerHTML = '<option value="">선택하세요</option>';
            groups.forEach(group => {
                const option = new Option(
                    `${group.groupName} (${group.companyName})`,
                    group.groupID
                );
                select.add(option);
            });
        }
    } catch (error) {
        console.error('센서 그룹 로드 실패:', error);
    }
}
async function activateSensor() {
    const sensorId = currentEditingSensorId || document.getElementById('editSensorId').value;

    if (!sensorId) {
        alert('센서 ID를 찾을 수 없습니다.');
        return;
    }

    if (!confirm('이 센서를 활성화하시겠습니까?')) return;

    try {
        const response = await fetch(`/api/sensors/${sensorId}/activate`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${getAuthToken()}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            alert('센서가 활성화되었습니다.');

            // 상태 업데이트
            const statusEl = document.getElementById('editSensorStatus');
            statusEl.textContent = '활성';
            statusEl.className = 'badge active';

            // 버튼 상태 업데이트
            updateActivationButtons('active');

            // 센서 목록 새로고침
            loadSensors();
        } else if (response.status === 404) {
            const error = await response.json();
            alert('센서를 찾을 수 없습니다: ' + (error.message || ''));
        } else {
            alert('센서 활성화 실패');
        }
    } catch (error) {
        alert('센서 활성화 중 오류가 발생했습니다: ' + error.message);
    }
}

// 센서 비활성화 함수
async function deactivateSensor() {
    const sensorId = currentEditingSensorId || document.getElementById('editSensorId').value;

    if (!sensorId) {
        alert('센서 ID를 찾을 수 없습니다.');
        return;
    }

    if (!confirm('이 센서를 비활성화하시겠습니까?\n비활성화된 센서는 데이터를 수집하지 않습니다.')) return;

    try {
        const response = await fetch(`/api/sensors/${sensorId}/deactivate`, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${getAuthToken()}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            alert('센서가 비활성화되었습니다.');

            // 상태 업데이트
            const statusEl = document.getElementById('editSensorStatus');
            statusEl.textContent = '비활성';
            statusEl.className = 'badge inactive';

            // 버튼 상태 업데이트
            updateActivationButtons('inactive');

            // 센서 목록 새로고침
            loadSensors();
        } else if (response.status === 404) {
            const error = await response.json();
            alert('센서를 찾을 수 없습니다: ' + (error.message || ''));
        } else {
            alert('센서 비활성화 실패');
        }
    } catch (error) {
        alert('센서 비활성화 중 오류가 발생했습니다: ' + error.message);
    }
}

// 센서 삭제 함수 (모달에서)
async function deleteSensorFromModal() {
    const sensorId = currentEditingSensorId || document.getElementById('editSensorId').value;

    if (!sensorId) {
        alert('센서 ID를 찾을 수 없습니다.');
        return;
    }

    if (!confirm('정말로 이 센서를 완전히 삭제하시겠습니까?\n\n⚠️ 주의: 이 작업은 되돌릴 수 없습니다.')) return;

    try {
        const response = await fetch(`/api/sensors/${sensorId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${getAuthToken()}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            alert('센서가 삭제되었습니다.');
            closeEditSensorModal();
            loadSensors();
        } else if (response.status === 404) {
            alert('센서를 찾을 수 없습니다.');
        } else if (response.status === 400) {
            const error = await response.json();
            alert('삭제 실패: ' + (error.message || '센서를 삭제할 수 없습니다.'));
        } else {
            alert('센서 삭제 중 오류가 발생했습니다.');
        }
    } catch (error) {
        alert('센서 삭제 실패: ' + error.message);
    }
}

// 센서 삭제 함수
//async function deleteSensor(sensorId) {
//    if (!confirm('정말로 이 센서를 완전히 삭제하시겠습니까?\n\n⚠️ 주의: 이 작업은 되돌릴 수 없습니다.')) return;

//    try {
//        const response = await fetch(`/api/sensors/${sensorId}`, {
//            method: 'DELETE',
//            headers: {
//                'Authorization': `Bearer ${getAuthToken()}`,
//                'Content-Type': 'application/json'
//            }
//        });

//        if (response.ok) {
//            alert('센서가 삭제되었습니다.');
//            loadSensors();
//        } else if (response.status === 404) {
//            alert('센서를 찾을 수 없습니다.');
//        } else if (response.status === 400) {
//            const error = await response.json();
//            alert('삭제 실패: ' + (error.message || '센서를 삭제할 수 없습니다.'));
//        } else {
//            alert('센서 삭제 중 오류가 발생했습니다.');
//        }
//    } catch (error) {
//        alert('센서 삭제 실패: ' + error.message);
//    }
//}

function generateNewUUID() {
    const uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = Math.random() * 16 | 0;
        const v = c === 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
    document.getElementById('newSensorUUID').value = uuid;
}
// 센서 그룹 로드
async function loadSensorGroups() {
    try {
        const response = await fetch('/api/sensor-groups', {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            const groups = await response.json();
            sensorGroups = groups;

            if (currentTab === 'sensor-groups') {
                updateSensorGroupStats();
                renderSensorGroups();
                updateGroupCompanyFilter();
            }
        }
    } catch (error) {
        console.error('센서 그룹 로드 실패:', error);
    }
}

// 센서 그룹 선택 옵션 업데이트
async function loadSensorGroupsForSelect() {
    try {
        const response = await fetch('/api/sensor-groups', {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            const groups = await response.json();
            const select = document.getElementById('sensorGroup');
            select.innerHTML = '<option value="">선택하세요</option>';

            groups.forEach(group => {
                const option = document.createElement('option');
                option.value = group.groupID;
                option.textContent = `${group.groupName} (${group.companyName})`;
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('센서 그룹 로드 실패:', error);
    }
}

// === 센서 그룹 관리 함수들 ===
function updateSensorGroupStats() {
    const activeGroups = sensorGroups.filter(g => g.active !== false);
    const totalSensors = sensorGroups.reduce((sum, g) => sum + (g.sensorCount || 0), 0);
    const avgSensors = sensorGroups.length > 0 ? (totalSensors / sensorGroups.length).toFixed(1) : 0;

    document.getElementById('totalSensorGroups').textContent = sensorGroups.length;
    document.getElementById('activeSensorGroups').textContent = activeGroups.length;
    document.getElementById('totalGroupSensors').textContent = totalSensors;
    document.getElementById('avgSensorsPerGroup').textContent = avgSensors;
}

function updateGroupCompanyFilter() {
    const select = document.getElementById('groupCompanyFilter');
    const currentValue = select.value;

    select.innerHTML = '<option value="">전체</option>';

    // 회사 목록에서 중복 제거
    const companyMap = new Map();
    sensorGroups.forEach(group => {
        if (group.companyID && group.companyName) {
            companyMap.set(group.companyID, group.companyName);
        }
    });

    companyMap.forEach((name, id) => {
        const option = document.createElement('option');
        option.value = id;
        option.textContent = name;
        select.appendChild(option);
    });

    select.value = currentValue;
}

function renderSensorGroups() {
    const grid = document.getElementById('sensorGroupGrid');
    const searchTerm = document.getElementById('groupSearchInput').value.toLowerCase();
    const companyFilter = document.getElementById('groupCompanyFilter').value;

    let filtered = sensorGroups.filter(group => {
        const matchesSearch = group.groupName?.toLowerCase().includes(searchTerm) ||
            group.location?.toLowerCase().includes(searchTerm) ||
            group.description?.toLowerCase().includes(searchTerm);

        const matchesCompany = !companyFilter || group.companyID?.toString() === companyFilter;

        return matchesSearch && matchesCompany;
    });

    if (filtered.length === 0) {
        grid.innerHTML = '<div class="empty-state">센서 그룹이 없습니다.</div>';
        return;
    }

    grid.innerHTML = filtered.map(group => `
                            <div class="sensor-group-card ${group.active === false ? 'inactive' : ''}">
                                <div class="sensor-group-header">
                                    <div class="sensor-group-name">${group.groupName}</div>
                                    <span class="badge ${group.active !== false ? 'active' : 'inactive'}">
                                        ${group.active !== false ? '활성' : '비활성'}
                                    </span>
                                </div>
                                <div class="sensor-group-body">
                                    <div class="sensor-group-info">
                                        <div><strong>회사:</strong> ${group.companyName || '-'}</div>
                                        <div><strong>위치:</strong> ${group.location || '-'}</div>
                                        <div><strong>설명:</strong> ${group.description || '-'}</div>
                                    </div>
                                    <div class="sensor-group-stats">
                                        <div>
                                            <div class="stat-value">${group.sensorCount || 0}</div>
                                            <div class="stat-label">센서</div>
                                        </div>
                                        <div>
                                            <div class="stat-value">${group.onlineSensorCount || 0}</div>
                                            <div class="stat-label">온라인</div>
                                        </div>
                                    </div>
                                    <div class="actions" style="margin-top: 1rem;">
                                        <button class="btn btn-sm" onclick="viewGroupSensors(${group.groupID})">센서 보기</button>
                                        <button class="btn btn-sm success" onclick="editGroupMembers(${group.groupID})">멤버 편집</button>
                                        <button class="btn btn-sm" onclick="editSensorGroup(${group.groupID})">그룹 수정</button>
                                        <button class="btn btn-sm danger" onclick="deleteSensorGroup(${group.groupID})">삭제</button>
                                    </div>
                                </div>
                            </div>
                        `).join('');
}

function searchGroups() {
    renderSensorGroups();
}

async function openAddGroupModal() {
    document.getElementById('sensorGroupModalTitle').textContent = '새 센서 그룹 추가';
    document.getElementById('sensorGroupForm').reset();
    document.getElementById('groupId').value = '';

    // 회사 목록 로드
    await loadCompaniesForGroupModal();

    document.getElementById('sensorGroupModal').style.display = 'block';
}

async function loadCompaniesForGroupModal() {
    try {
        const response = await fetch('/api/companies', {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            const companies = await response.json();
            const select = document.getElementById('groupCompany');
            select.innerHTML = '<option value="">선택하세요</option>';

            companies.forEach(company => {
                const option = document.createElement('option');
                option.value = company.companyID;
                option.textContent = company.companyName;
                if (!company.active) {
                    option.textContent += ' (비활성)';
                    option.disabled = true;
                }
                select.appendChild(option);
            });
        }
    } catch (error) {
        console.error('회사 목록 로드 실패:', error);
    }
}

async function editSensorGroup(groupId) {
    try {
        const response = await fetch(`/api/sensor-groups/${groupId}`, {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            const group = await response.json();

            document.getElementById('sensorGroupModalTitle').textContent = '센서 그룹 수정';
            document.getElementById('groupId').value = group.groupID;
            document.getElementById('groupName').value = group.groupName;
            document.getElementById('groupLocation').value = group.location || '';
            document.getElementById('groupDescription').value = group.description || '';
            document.getElementById('groupActive').checked = group.active !== false;

            await loadCompaniesForGroupModal();
            document.getElementById('groupCompany').value = group.companyID;

            document.getElementById('sensorGroupModal').style.display = 'block';
        }
    } catch (error) {
        alert('센서 그룹 정보 로드 실패: ' + error.message);
    }
}

function closeSensorGroupModal() {
    document.getElementById('sensorGroupModal').style.display = 'none';
    document.getElementById('sensorGroupForm').reset();
}

async function deleteSensorGroup(groupId) {
    if (!confirm('정말로 이 센서 그룹을 삭제하시겠습니까?\n\n⚠️ 주의: 그룹에 센서가 있으면 삭제할 수 없습니다.')) {
        return;
    }

    try {
        const response = await fetch(`/api/sensor-groups/${groupId}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            alert('센서 그룹이 삭제되었습니다.');
            loadSensorGroups();
        } else if (response.status === 400) {
            alert('이 그룹에는 센서가 있어 삭제할 수 없습니다.');
        } else {
            const errorText = await response.text();
            alert('삭제 실패: ' + errorText);
        }
    } catch (error) {
        alert('센서 그룹 삭제 실패: ' + error.message);
    }
}

async function viewGroupSensors(groupId) {
    try {
        const response = await fetch(`/api/sensor-groups/${groupId}/sensors`, {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            const sensors = await response.json();

            let message = '그룹 내 센서 목록:\n\n';
            if (sensors.length === 0) {
                message += '센서가 없습니다.';
            } else {
                sensors.forEach(sensor => {
                    message += `- ${sensor.name || sensor.sensorUUID} (${sensor.sensorType}, ${sensor.connectionStatus})\n`;
                });
            }

            alert(message);
        }
    } catch (error) {
        alert('센서 목록 조회 실패: ' + error.message);
    }
}

// 센서 그룹 폼 제출
document.getElementById('sensorGroupForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const groupId = document.getElementById('groupId').value;
    const isNew = !groupId;

    const groupData = {
        GroupName: document.getElementById('groupName').value,
        CompanyID: parseInt(document.getElementById('groupCompany').value),
        Location: document.getElementById('groupLocation').value || null,
        Description: document.getElementById('groupDescription').value || "기본",
        Active: document.getElementById('groupActive').checked,
    };

    try {
        const response = await fetch(isNew ? '/api/sensor-groups' : `/api/sensor-groups/${groupId}`, {
            method: isNew ? 'POST' : 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getAuthToken()}`
            },
            body: JSON.stringify(groupData)
        });

        if (response.ok) {
            alert(isNew ? '센서 그룹이 생성되었습니다.' : '센서 그룹이 수정되었습니다.');
            closeSensorGroupModal();
            loadSensorGroups();
        } else {
            const errorText = await response.text();
            alert('저장 실패: ' + errorText);
        }
    } catch (error) {
        alert('저장 중 오류가 발생했습니다: ' + error.message);
    }
});

// 센서 수정 시 폼에 데이터 로드하는 함수도 수정
async function editSensor(sensorId) {
    try {
        const response = await fetch(`/api/sensors/${sensorId}`, {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            const sensor = await response.json();

            document.getElementById('sensorModalTitle').textContent = '센서 수정';
            document.getElementById('sensorId').value = sensor.sensorID;
            document.getElementById('sensorName').value = sensor.name || '';
            document.getElementById('sensorType').value = sensor.sensorType;
            document.getElementById('sensorType').disabled = true; // 타입은 수정 불가
            document.getElementById('sensorModel').value = sensor.model || '';
            document.getElementById('firmwareVersion').value = sensor.firmwareVersion || '1.0.0';
            document.getElementById('heartbeatInterval').value = sensor.heartbeatInterval || 60;
            document.getElementById('connectionTimeout').value = sensor.connectionTimeout || 180;
            document.getElementById('sensorStatus').value = sensor.status || 'active';


            // UUID 필드는 수정 시 표시만 하고 수정 불가
            if (document.getElementById('sensorUUID')) {
                document.getElementById('sensorUUID').value = sensor.sensorUUID;
                document.getElementById('sensorUUID').disabled = true;
            }

            document.getElementById('sensorModal').style.display = 'block';
        }
    } catch (error) {
        alert('센서 정보 로드 실패: ' + error.message);
    }
}

// 센서 모달 닫기
function closeSensorModal() {
    document.getElementById('sensorModal').style.display = 'none';
    document.getElementById('sensorForm').reset();
}

// === 폼 제출 핸들러 ===
document.getElementById('addSensorForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const sensorType = document.getElementById('newSensorType').value;

    const sensorData = {
        sensorUUID: document.getElementById('newSensorUUID').value,
        name: document.getElementById('newSensorName').value,
        sensorType: sensorType,
        groupID: parseInt(document.getElementById('newSensorGroup').value),
        model: document.getElementById('newSensorModel').value || null,
        heartbeatInterval: parseInt(document.getElementById('newHeartbeatInterval').value) || 60,
        connectionTimeout: parseInt(document.getElementById('newConnectionTimeout').value) || 180,
        installationDate: document.getElementById('newInstallationDate').value || null,
        firmwareVersion: '1.0.0',  // 기본값 설정
        mqttTopics: {  // MQTT 토픽 정보 추가
            dataTopic: `/sensors/${sensorType}/data`,
            controlTopic: `/sensors/${sensorType}/control`,
            statusTopic: `/sensors/${sensorType}/status`,
            heartbeatTopic: `/sensors/${sensorType}/heartbeat`
        }
    };

    try {
        const response = await fetch('/api/sensors', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getAuthToken()}`
            },
            body: JSON.stringify(sensorData)
        });

        if (response.ok) {
            alert('센서가 추가되었습니다.');
            closeAddSensorModal();
            loadSensors(); // 목록 새로고침
        } else {
            const error = await response.text();
            alert('센서 추가 실패: ' + error);
        }
    } catch (error) {
        alert('센서 추가 중 오류가 발생했습니다: ' + error.message);
    }
});

document.getElementById('editSensorForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const sensorId = document.getElementById('editSensorId').value;
    const updateData = {
        name: document.getElementById('editSensorName').value,
        groupID: parseInt(document.getElementById('editSensorGroup').value),
        model: document.getElementById('editSensorModel').value || null,
        heartbeatInterval: parseInt(document.getElementById('editHeartbeatInterval').value) || 60,
        connectionTimeout: parseInt(document.getElementById('editConnectionTimeout').value) || 180,
        firmwareVersion: document.getElementById('editFirmwareVersion').value || null

    };

    try {
        const response = await fetch(`/api/sensors/${sensorId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getAuthToken()}`
            },
            body: JSON.stringify(updateData)
        });

        if (response.ok) {
            alert('센서 정보가 수정되었습니다.');
            closeEditSensorModal();
            loadSensors(); // 목록 새로고침
        } else {
            const error = await response.text();
            alert('센서 수정 실패: ' + error);
        }
    } catch (error) {
        alert('센서 수정 중 오류가 발생했습니다: ' + error.message);
    }
});

// 센서 타입 정보 헬퍼
function getSensorTypeInfo(sensorType) {
    const types = {
        'temp_humidity': { icon: '🌡️', name: '온습도 센서' },
        'particle': { icon: '💨', name: '미세먼지 센서' },
        'wind': { icon: '🌪️', name: '풍속 센서' },
        'speaker': { icon: '🔊', name: '스피커' }
    };
    return types[sensorType] || { icon: '📡', name: '알 수 없음' };
}

// 센서 폼 제출
document.getElementById('sensorForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const sensorId = document.getElementById('sensorId').value;
    const isNew = !sensorId;

    // UUID 생성 함수
    function generateUUID() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
            var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    let sensorData;

    if (isNew) {
        // 신규 생성 시
        sensorData = {
            name: document.getElementById('sensorName').value,
            sensorType: document.getElementById('sensorType').value,
            sensorUUID: generateUUID(),  // UUID 자동 생성
            groupID: parseInt(document.getElementById('sensorGroup').value),
            model: document.getElementById('sensorModel').value || '',
            firmwareVersion: '1.0.0',  // 기본값 설정
            heartbeatInterval: parseInt(document.getElementById('heartbeatInterval').value) || 60,
            status: 'active',
            mqttTopics: {  // MQTT 토픽 정보 추가
                dataTopic: `/sensors/${document.getElementById('sensorType').value}/data`,
                controlTopic: `/sensors/${document.getElementById('sensorType').value}/control`,
                statusTopic: `/sensors/${document.getElementById('sensorType').value}/status`,
                heartbeatTopic: `/sensors/${document.getElementById('sensorType').value}/heartbeat`
            }
        };
    } else {
        // 수정 시 - 수정 가능한 필드만 전송
        sensorData = {
            name: document.getElementById('sensorName').value,
            model: document.getElementById('sensorModel').value || '',
            firmwareVersion: document.getElementById('firmwareVersion').value || '1.0.0',
            heartbeatInterval: parseInt(document.getElementById('heartbeatInterval').value) || 60,
            connectionTimeout: parseInt(document.getElementById('connectionTimeout').value) || 180,
            status: document.getElementById('sensorStatus').value || 'active',
            groupID: parseInt(document.getElementById('sensorGroup').value)
        };
    }

    try {
        const response = await fetch(isNew ? '/api/sensors' : `/api/sensors/${sensorId}`, {
            method: isNew ? 'POST' : 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getAuthToken()}`
            },
            body: JSON.stringify(sensorData)
        });

        if (response.ok) {
            alert(isNew ? '센서가 생성되었습니다.' : '센서 정보가 수정되었습니다.');
            closeSensorModal();
            loadSensors();
        } else {
            const errorText = await response.text();
            console.error('Error response:', errorText);

            // 에러 메시지 파싱
            try {
                const errorObj = JSON.parse(errorText);
                if (errorObj.errors) {
                    let errorMessage = '다음 항목을 확인해주세요:\n';
                    for (const [field, messages] of Object.entries(errorObj.errors)) {
                        errorMessage += `\n${field}: ${messages.join(', ')}`;
                    }
                    alert(errorMessage);
                } else {
                    alert('저장 실패: ' + (errorObj.message || errorText));
                }
            } catch {
                alert('저장 실패: ' + errorText);
            }
        }
    } catch (error) {
        alert('저장 중 오류가 발생했습니다: ' + error.message);
    }
});

// === 사용자 관리 함수들 ===
async function loadUsers() {
    const includeInactive = document.getElementById('showInactive').checked;

    try {
        console.log('Loading users...');
        console.log('Token:', getAuthToken());

        const response = await fetch(`/api/users?includeInactive=${includeInactive}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${getAuthToken()}`,
                'Content-Type': 'application/json'
            }
        });

        console.log('Response status:', response.status);

        if (response.status === 401) {
            alert('인증이 만료되었습니다. 다시 로그인해주세요.');
            window.location.href = '/login.html';
            return;
        }

        if (!response.ok) {
            const errorText = await response.text();
            console.error('Error response:', errorText);
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        console.log('Received data:', data);

        users = data;
        updateUserStats();
        renderUsers();

    } catch (error) {
        console.error('사용자 로드 실패:', error);

        // 더 자세한 에러 메시지
        if (error.message.includes('Failed to fetch')) {
            alert('서버에 연결할 수 없습니다. API 서버가 실행 중인지 확인하세요.');
        } else {
            alert(`사용자 목록을 불러오는데 실패했습니다: ${error.message}`);
        }
    }
}

async function loadCompanies() {
    try {
        console.log('Loading companies...');
        const token = getAuthToken();

        if (!token) {
            console.error('No auth token found');
            alert('인증 토큰이 없습니다. 다시 로그인해주세요.');
            window.location.href = '/login.html';
            return;
        }

        const response = await fetch('/api/companies?includeInactive=true', {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        console.log('Companies response status:', response.status);


        if (response.ok) {
            companies = await response.json();
            console.log('Loaded companies:', companies);

            // 첫 번째 회사 객체의 구조 확인
            if (companies.length > 0) {
                console.log('Sample company object:', companies[0]);
                console.log('Company properties:', Object.keys(companies[0]));
            }

            if (currentTab === 'companies') {
                updateCompanyStats();
                renderCompanies();
            } else if (currentTab === 'users') {
                updateCompanySelect();
            }
        } else {
            const errorText = await response.text();
            console.error('Failed to load companies:', errorText);
        }
    } catch (error) {
        console.error('회사 로드 실패:', error);
        companies = [];
    }
}

function updateCompanySelect() {
    const select = document.getElementById('companies');

    console.log('updateCompanySelect called');
    console.log('Companies array:', companies);

    // select 요소가 존재하는지 확인
    if (!select) {
        console.error('companies select element not found');
        return;
    }

    select.innerHTML = '';

    if (!companies || companies.length === 0) {
        const option = new Option('등록된 회사가 없습니다', '');
        option.disabled = true;
        select.add(option);
        return;
    }

    companies.forEach((company, index) => {
        console.log(`Company ${index}:`, company);

        // 회사 객체의 모든 속성 확인
        console.log('Company properties:', Object.keys(company));

        const option = document.createElement('option');

        // 다양한 속성명 시도
        const companyName = company.companyName || 'Unknown';  // companyName 사용
        const companyId = company.companyID;  // companyID 사용 (대문자 ID)


        option.text = companyName;
        option.value = companyId;

        if (company.isActive === false) {
            option.text += ' (비활성)';
            option.style.color = '#999';
        }

        select.add(option);
    });

    console.log('Company select updated successfully');
}

function updateUserStats() {
    document.getElementById('totalUsers').textContent = users.length;
    document.getElementById('activeUsers').textContent = users.filter(u => u.isActive).length;
    document.getElementById('adminUsers').textContent = users.filter(u => u.role === 'Admin').length;
    document.getElementById('inactiveUsers').textContent = users.filter(u => !u.isActive).length;
}

function renderUsers() {
    const tbody = document.getElementById('userTableBody');
    const searchTerm = document.getElementById('userSearchInput').value.toLowerCase();

    const filtered = users.filter(user =>
        user.username?.toLowerCase().includes(searchTerm) ||
        user.email?.toLowerCase().includes(searchTerm) ||
        user.name?.toLowerCase().includes(searchTerm)
    );

    document.getElementById('userCount').textContent = `총 ${filtered.length}명의 사용자`;

    if (filtered.length === 0) {
        tbody.innerHTML = '<tr><td colspan="9" class="empty-state">사용자가 없습니다.</td></tr>';
        return;
    }

    tbody.innerHTML = filtered.map(user => `
                            <tr>
                                <td>${user.userID}</td>
                                <td>${user.username}</td>
                                <td>${user.fullName || user.name || '-'}</td>  <!-- fullName 우선 표시 -->
                                <td>${user.email}</td>
                                <td><span class="badge ${user.role?.toLowerCase()}">${user.role}</span></td>
                                <td>${user.companyNames || user.companyName || '-'}</td>
                                <td><span class="badge ${user.isActive ? 'active' : 'inactive'}">${user.isActive ? '활성' : '비활성'}</span></td>
                                <td>${new Date(user.createdAt).toLocaleDateString('ko-KR')}</td>
                                <td>
                                    <div class="actions">
                                        <button class="btn btn-sm" onclick="editUser(${user.userID})">수정</button>
                                        <button class="btn btn-sm warning" onclick="showResetPasswordModal(${user.userID})">비밀번호</button>
                                        ${user.isActive ?
            `<button class="btn btn-sm danger" onclick="toggleUserStatus(${user.userID}, false)">비활성화</button>` :
            `<button class="btn btn-sm success" onclick="toggleUserStatus(${user.userID}, true)">활성화</button>`
        }
                                        <button class="btn btn-sm danger" onclick="deleteUser(${user.userID}, '${user.username}')">삭제</button>
                                    </div>
                                </td>
                            </tr>
                        `).join('');
}

function searchUsers() {
    renderUsers();
}

function openAddUserModal() {
    document.getElementById('userModalTitle').textContent = '새 사용자 추가';
    document.getElementById('userForm').reset();
    document.getElementById('userId').value = '';
    document.getElementById('username').disabled = false;
    document.getElementById('passwordGroup').style.display = 'block';
    document.getElementById('password').required = true;

    // ✅ 회사 목록 로드 및 업데이트
    loadCompanies().then(() => {
        updateCompanySelect();
    });

    document.getElementById('userModal').style.display = 'block';
}

async function deleteUser(userId, username) {
    // 삭제 확인 대화상자
    const confirmMessage = `정말로 사용자 "${username}"을(를) 완전히 삭제하시겠습니까?\n\n⚠️ 주의: 이 작업은 되돌릴 수 없습니다.`;

    if (!confirm(confirmMessage)) {
        return;
    }

    // 한 번 더 확인 (중요한 작업이므로)
    const secondConfirm = prompt(`삭제를 확인하려면 사용자명 "${username}"을(를) 입력하세요:`);

    if (secondConfirm !== username) {
        alert('사용자명이 일치하지 않습니다. 삭제가 취소되었습니다.');
        return;
    }

    try {
        console.log(`Deleting user ${userId}...`);

        const response = await fetch(`/api/users/${userId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${getAuthToken()}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            alert(`사용자 "${username}"이(가) 성공적으로 삭제되었습니다.`);

            // 사용자 목록 새로고침
            await loadUsers();
        } else if (response.status === 403) {
            alert('권한이 없습니다. 관리자만 사용자를 삭제할 수 있습니다.');
        } else if (response.status === 404) {
            alert('사용자를 찾을 수 없습니다.');
        } else {
            const errorText = await response.text();
            console.error('Delete error:', errorText);

            try {
                const errorObj = JSON.parse(errorText);
                alert(`삭제 실패: ${errorObj.message || errorObj.title || '알 수 없는 오류'}`);
            } catch {
                alert(`삭제 실패: ${errorText}`);
            }
        }
    } catch (error) {
        console.error('사용자 삭제 중 오류:', error);
        alert('사용자 삭제 중 오류가 발생했습니다: ' + error.message);
    }
}

async function editUser(userId) {
    try {
        const response = await fetch(`/api/users/${userId}`, {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (response.ok) {
            const user = await response.json();

            document.getElementById('userModalTitle').textContent = '사용자 수정';
            document.getElementById('userId').value = user.userID;
            document.getElementById('username').value = user.username;
            document.getElementById('username').disabled = true;
            document.getElementById('passwordGroup').style.display = 'none';
            document.getElementById('password').required = false;
            document.getElementById('fullName').value = user.fullName || user.name || '';
            document.getElementById('email').value = user.email;
            document.getElementById('userphone').value = user.phone || '';
            document.getElementById('active').checked = user.isActive;

            // 역할 설정
            document.getElementById('roles').value = user.role || 'User';

            await loadCompanies();
            updateCompanySelect();

            setTimeout(() => {
                const companiesSelect = document.getElementById('companies');
                if (user.companyIDs && companiesSelect) {
                    // 모든 옵션의 선택 상태 초기화
                    Array.from(companiesSelect.options).forEach(option => {
                        option.selected = false;
                    });

                    // 사용자의 회사 ID들을 선택
                    user.companyIDs.forEach(companyId => {
                        Array.from(companiesSelect.options).forEach(option => {
                            if (parseInt(option.value) === companyId) {
                                option.selected = true;
                            }
                        });
                    });
                } else if (user.companyID && companiesSelect) {
                    // 단일 회사 ID인 경우
                    companiesSelect.value = user.companyID.toString();
                }
            }, 100); // 회사 목록 로드 완료 대기

            document.getElementById('userModal').style.display = 'block';
        }
    } catch (error) {
        alert('사용자 정보 로드 실패: ' + error.message);
    }
}

function closeUserModal() {
    document.getElementById('userModal').style.display = 'none';
    document.getElementById('userForm').reset();
}

function showResetPasswordModal(userId) {
    document.getElementById('resetUserId').value = userId;
    document.getElementById('newPassword').value = '';
    document.getElementById('resetPasswordModal').style.display = 'block';
}

function closeResetModal() {
    document.getElementById('resetPasswordModal').style.display = 'none';
}

async function resetPassword() {
    const userId = document.getElementById('resetUserId').value;
    const newPassword = document.getElementById('newPassword').value;

    if (!newPassword) {
        alert('새 비밀번호를 입력하세요.');
        return;
    }

    try {
        const response = await fetch(`/api/users/${userId}/reset-password`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getAuthToken()}`
            },
            body: JSON.stringify({ newPassword })
        });

        if (response.ok) {
            alert('비밀번호가 리셋되었습니다.');
            closeResetModal();
        }
    } catch (error) {
        alert('비밀번호 리셋 실패: ' + error.message);
    }
}

async function toggleUserStatus(userId, activate) {
    const action = activate ? '활성화' : '비활성화';
    const confirmMessage = `이 사용자를 ${action}하시겠습니까?`;

    if (!confirm(confirmMessage)) {
        return;
    }

    try {
        const endpoint = activate
            ? `/api/users/${userId}/activate`
            : `/api/users/${userId}/deactivate`;

        const response = await fetch(endpoint, {
            method: 'PUT',
            headers: {
                'Authorization': `Bearer ${getAuthToken()}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            alert(`사용자가 ${action}되었습니다.`);
            await loadUsers();
        } else {
            const errorText = await response.text();
            alert(`${action} 실패: ${errorText}`);
        }
    } catch (error) {
        console.error(`${action} 중 오류:`, error);
        alert(`${action} 중 오류가 발생했습니다: ${error.message}`);
    }
}

// === 회사 관리 함수들 ===
function updateCompanyStats() {
    document.getElementById('totalCompanies').textContent = companies.length;
    document.getElementById('activeCompanies').textContent = companies.filter(c => c.isActive).length;
    document.getElementById('inactiveCompanies').textContent = companies.filter(c => !c.isActive).length;

    // 배정된 사용자 수 계산 (실제로는 API에서 가져와야 함)
    document.getElementById('companyUsers').textContent = users.filter(u => u.companyID).length;
}

function renderCompanies() {
    const grid = document.getElementById('companyGrid');
    const searchTerm = document.getElementById('companySearchInput').value.toLowerCase();

    const filtered = companies.filter(company =>
        company.companyName?.toLowerCase().includes(searchTerm) ||
        company.contactPerson?.toLowerCase().includes(searchTerm) ||
        company.address?.toLowerCase().includes(searchTerm)
    );

    if (filtered.length === 0) {
        grid.innerHTML = '<div class="empty-state">회사가 없습니다.</div>';
        return;
    }

    grid.innerHTML = filtered.map(company => `
                            <div class="company-card">
                                <div class="company-header">
                                    <div class="company-name">${company.companyName}</div>
                                    <span class="badge ${company.active ? 'active' : 'inactive'}">
                                        ${company.active ? '활성' : '비활성'}
                                    </span>
                                </div>
                                <div class="company-body">
                                    <div class="company-info">
                                        <div><strong>담당자:</strong> ${company.contactPerson || '-'}</div>
                                        <div><strong>주소:</strong> ${company.address || '-'}</div>
                                        <div><strong>전화:</strong> ${company.contactPhone || '-'}</div>
                                        <div><strong>이메일:</strong> ${company.contactEmail || '-'}</div>
                                    </div>
                                    <div class="company-stats">
                                        <div>
                                            <div class="company-stat-value">${company.sensorCount || 0}</div>
                                            <div class="company-stat-label">센서</div>
                                        </div>
                                        <div>
                                            <div class="company-stat-value">${company.groupCount || 0}</div>
                                            <div class="company-stat-label">그룹</div>
                                        </div>
                                        <div>
                                            <div class="company-stat-value">${company.userCount || 0}</div>
                                            <div class="company-stat-label">사용자</div>
                                        </div>
                                    </div>
                                    <div class="actions" style="margin-top: 1rem;">
                                        <button class="btn btn-sm" onclick="editCompany(${company.companyID})">수정</button>
                                        ${company.active ?
            `<button class="btn btn-sm warning" onclick="toggleCompanyStatus(${company.companyID}, false)">비활성화</button>` :
            `<button class="btn btn-sm success" onclick="toggleCompanyStatus(${company.companyID}, true)">활성화</button>`
        }
                                        <button class="btn btn-sm danger" onclick="deleteCompany(${company.companyID})">삭제</button>
                                    </div>
                                </div>
                            </div>
                        `).join('');
}

async function toggleCompanyStatus(companyId, activate) {
    const action = activate ? '활성화' : '비활성화';
    const endpoint = activate ?
        `/api/companies/${companyId}/activate` :
        `/api/companies/${companyId}/deactivate`;

    if (!confirm(`이 회사를 ${action}하시겠습니까?`)) {
        return;
    }

    try {
        const response = await fetch(endpoint, {
            method: 'POST',  // ✅ PUT이 아닌 POST 사용
            headers: {
                'Authorization': `Bearer ${getAuthToken()}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            alert(`회사가 ${action}되었습니다.`);
            await loadCompanies();
        } else {
            const errorText = await response.text();
            alert(`${action} 실패: ${errorText}`);
        }
    } catch (error) {
        console.error(`Company ${action} error:`, error);
        alert(`회사 ${action} 중 오류가 발생했습니다.`);
    }
}

function searchCompanies() {
    renderCompanies();
}

function openAddCompanyModal() {
    // 모달 제목 설정
    // 타이틀 요소가 있는지 확인
    console.warn('new complete!!!!!');

    const titleElement = document.getElementById('companyModalTitle');
    if (titleElement) {
        titleElement.textContent = '새 회사 추가';
    } else {
        console.warn('companyModalTitle element not found');
    }

    // 폼 초기화
    document.getElementById('companyForm').reset();
    document.getElementById('companyId').value = ''; // hidden 필드 초기화
    document.getElementById('companyActive').checked = true; // 기본값 활성

    // 모달 표시
    document.getElementById('companyModal').style.display = 'block';

    console.warn('new complete!!!!! end');
}

function checkCompanyModalStructure() {
    const modal = document.getElementById('companyModal');
    const title = document.getElementById('companyModalTitle');
    const form = document.getElementById('companyForm');

    console.log('Modal exists:', !!modal);
    console.log('Title exists:', !!title);
    console.log('Form exists:', !!form);

    if (!title) {
        console.log('Creating modal title element...');
        // 타이틀이 없다면 동적으로 생성
        const modalHeader = modal.querySelector('.modal-header');
        if (modalHeader && !modalHeader.querySelector('#companyModalTitle')) {
            const h2 = document.createElement('h2');
            h2.id = 'companyModalTitle';
            h2.textContent = '새 회사 추가';
            modalHeader.insertBefore(h2, modalHeader.firstChild);
        }
    }
}

async function editCompany(companyId) {
    try {
        // 회사 정보 로드
        const response = await fetch(`/api/companies/${companyId}`, {
            headers: { 'Authorization': `Bearer ${getAuthToken()}` }
        });

        if (!response.ok) {
            throw new Error('회사 정보를 불러올 수 없습니다.');
        }

        const company = await response.json();
        console.log('Loaded company:', company);

        // 모달 제목 변경
        const titleElement = document.getElementById('companyModalTitle');
        if (titleElement) {
            titleElement.textContent = '회사 수정';
        }


        // 폼 필드에 데이터 채우기
        document.getElementById('companyId').value = company.companyID;
        document.getElementById('companyName').value = company.companyName || '';
        document.getElementById('contactPerson').value = company.contactPerson || '';
        document.getElementById('address').value = company.address || '';
        document.getElementById('companyphone').value = company.contactPhone || '';
        document.getElementById('companyEmail').value = company.contactEmail || '';
        document.getElementById('companyActive').checked = company.active;

        // 모달 표시
        document.getElementById('companyModal').style.display = 'block';
    } catch (error) {
        console.error('Error loading company:', error);
        alert('회사 정보 로드 실패: ' + error.message);
    }
}

function closeCompanyModal() {
    document.getElementById('companyModal').style.display = 'none';
    document.getElementById('companyForm').reset();
    document.getElementById('companyId').value = ''; // hidden 필드도 초기화
}

async function deleteCompany(companyId) {
    // 삭제 전 확인
    if (!confirm('정말로 이 회사를 삭제하시겠습니까?\n\n⚠️ 주의: 이 작업은 되돌릴 수 없습니다.')) {
        return;
    }

    try {
        const response = await fetch(`/api/companies/${companyId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': `Bearer ${getAuthToken()}`,
                'Content-Type': 'application/json'
            }
        });

        if (response.ok) {
            alert('회사가 삭제되었습니다.');
            await loadCompanies(); // 목록 새로고침
        } else if (response.status === 400) {
            alert('이 회사는 사용 중이므로 삭제할 수 없습니다.');
        } else {
            const errorText = await response.text();
            alert('삭제 실패: ' + errorText);
        }
    } catch (error) {
        console.error('Error deleting company:', error);
        alert('회사 삭제 중 오류가 발생했습니다: ' + error.message);
    }
}

async function handleUserSubmit(e) {
    e.preventDefault();

    const userId = document.getElementById('userId').value;
    const isNew = !userId;

    const isActiveValue = document.getElementById('active').checked ? true : false;

    // API 요구사항에 맞게 필드명 수정
    const userData = {
        username: document.getElementById('username').value,
        fullName: document.getElementById('fullName').value,
        email: document.getElementById('email').value,
        phone: document.getElementById('userphone').value || '',
        password: document.getElementById('password').value,
        role: document.getElementById('roles').value,
        companyIDs: [],
        isActive: isActiveValue
    };

    // 회사 선택 처리 (다중 선택인 경우)
    const companiesSelect = document.getElementById('companies');
    if (companiesSelect) {
        userData.companyIDs = Array.from(companiesSelect.selectedOptions)
            .map(option => parseInt(option.value))
            .filter(id => !isNaN(id));
    }

    // 새 사용자인 경우 비밀번호 추가
    if (isNew) {
        userData.password = document.getElementById('password').value;

        // 필수 필드 검증
        if (!userData.password) {
            alert('비밀번호를 입력해주세요.');
            return;
        }
    }

    console.log('Saving user data:', userData); // 디버깅용

    // 필수 필드 검증
    if (!userData.fullName) {
        alert('전체 이름을 입력해주세요.');
        return;
    }

    if (!userData.role) {
        alert('역할을 선택해주세요.');
        return;
    }


    console.log('전송할 데이터:', userData);

    try {
        const response = await fetch(isNew ? '/api/users' : `/api/users/${userId}`, {
            method: isNew ? 'POST' : 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getAuthToken()}`
            },
            body: JSON.stringify(userData)
        });

        if (response.ok) {
            alert(isNew ? '사용자가 생성되었습니다.' : '사용자 정보가 수정되었습니다.');
            closeUserModal();
            loadUsers();
        } else {
            const errorText = await response.text();
            console.error('저장 실패:', errorText);

            try {
                const errorObj = JSON.parse(errorText);
                if (errorObj.errors) {
                    let errorMessage = '다음 항목을 확인해주세요:\n';
                    for (const [field, messages] of Object.entries(errorObj.errors)) {
                        errorMessage += `\n${field}: ${messages.join(', ')}`;
                    }
                    alert(errorMessage);
                } else {
                    alert('저장 실패: ' + (errorObj.title || errorText));
                }
            } catch {
                alert('저장 실패: ' + errorText);
            }
        }
    } catch (error) {
        alert('저장 중 오류가 발생했습니다: ' + error.message);
    }
}

async function handleCompanySubmit(e) {
    e.preventDefault();

    console.log('현재 탭 상태:', currentTab); // 디버깅용 추가

    const companyId = document.getElementById('companyId').value;
    const isNew = !companyId;

    // 각 필드 값을 개별적으로 확인
    const companyName = document.getElementById('companyName').value;
    const contactPerson = document.getElementById('contactPerson').value || '';
    const address = document.getElementById('address').value || '';
    const phone = document.getElementById('companyphone').value || '';
    const companyEmail = document.getElementById('companyEmail').value || '';
    const companyActive = document.getElementById('companyActive').checked;

    // 디버깅: 각 필드 값 출력
    console.log('Field values:', {
        companyName,
        contactPerson,
        address,
        phone,  // 이 값이 제대로 나오는지 확인
        companyEmail,
        companyActive
    });

    const companyData = {
        CompanyName: companyName,
        ContactPerson: contactPerson,
        Address: address,
        ContactPhone: phone,  // ✅ phone 변수 사용
        ContactEmail: companyEmail,
        Active: companyActive
    };


    // 필수 필드 검증
    if (!companyData.CompanyName) {
        alert('회사명을 입력해주세요.');
        return;
    }

    console.log('Saving company:', { isNew, companyId, data: companyData });

    try {
        const response = await fetch(isNew ? '/api/companies' : `/api/companies/${companyId}`, {
            method: isNew ? 'POST' : 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getAuthToken()}`
            },
            body: JSON.stringify(companyData)
        });


        if (response.ok || response.status === 201) {
            alert(isNew ? '회사가 생성되었습니다.' : '회사 정보가 수정되었습니다.');
            closeCompanyModal();
            loadCompanies();
        } else {
            // 에러 처리
            let errorMessage = '저장 실패';

            try {
                const errorText = await response.text();
                console.error('Error response:', errorText);

                if (errorText) {
                    try {
                        const errorObj = JSON.parse(errorText);
                        errorMessage = errorObj.message || errorObj.title || errorText;
                    } catch {
                        errorMessage = errorText;
                    }
                }
            } catch (e) {
                console.error('Error reading response:', e);
            }

            alert(errorMessage);
        }
    } catch (error) {
        console.error('Save error:', error);
        alert('저장 중 오류가 발생했습니다: ' + error.message);
    }
}

// 모달 외부 클릭 시 닫기
window.onclick = function (event) {
    if (event.target == document.getElementById('sensorModal')) {
        closeSensorModal();
    }
    if (event.target == document.getElementById('userModal')) {
        closeUserModal();
    }
    if (event.target == document.getElementById('resetPasswordModal')) {
        closeResetModal();
    }
    if (event.target == document.getElementById('companyModal')) {
        closeCompanyModal();
    }
    if (event.target == document.getElementById('sensorGroupModal')) {
        closeSensorGroupModal();
    }
    if (event.target == document.getElementById('sensorGroupMemberModal')) {
        closeSensorGroupMemberModal();
    }
}