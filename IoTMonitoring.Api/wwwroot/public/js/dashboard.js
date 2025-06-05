// 전역 변수
let connection = null;
let temperatureChart = null;
let humidityChart = null;
let particleChart = null;
let sensorStates = new Map();
let companies = [];
let selectedCompanyId = null;
let userRole = 'User'; // 기본값
let isAdmin = false;

// 통계 데이터
let adminStats = {
    users: { total: 0, active: 0, inactive: 0 },
    companies: { total: 0, active: 0, inactive: 0 },
    sensors: { total: 0, online: 0, tempHumidity: 0, particle: 0 },
    alerts: { active: 0, today: 0, week: 0 }
};

// 센서 타입별 아이콘 및 이름 매핑
const sensorTypeInfo = {
    'particle': { icon: '💨', name: '미세먼지 센서' },
    'temp_humidity': { icon: '🌡️', name: '온습도 센서' },
    'wind': { icon: '🌪️', name: '풍향 센서' },
    'speaker': { icon: '🔊', name: '스피커' }
};

// 센서 타입 정보 가져오기
function getSensorTypeInfo(sensorType) {
    return sensorTypeInfo[sensorType] || { icon: '📡', name: '일반 센서' };
}

// 관리자 페이지 열기 함수들
function openUserManagement() {
    window.location.href = '/admin-management.html#users';
}
function openSensorGroupManagement() {
    window.location.href = '/admin-management.html#sensor-groups';
}
function openCompanyManagement() {
    window.location.href = '/admin-management.html#companies';
}

function openSensorManagement() {
    window.location.href = '/admin-management.html#sensors';
}

// 3. 프로필 페이지 열기 함수 추가
function openProfile() {
    window.location.href = '/user-profile.html';
}

function viewSystemLogs() {
    alert('시스템 로그 기능은 준비 중입니다.');
}

// 토큰 만료 확인 함수
function isTokenExpired() {
    const expiration = localStorage.getItem('expiration');
    if (!expiration) return true;

    const expirationDate = new Date(expiration);
    return new Date() >= expirationDate;
}

// 토큰 갱신 함수
async function refreshToken() {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) {
        redirectToLogin();
        return null;
    }

    try {
        const response = await fetch('/api/auth/refresh', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(refreshToken)
        });

        if (response.ok) {
            const data = await response.json();

            localStorage.setItem('authToken', data.token);
            localStorage.setItem('refreshToken', data.refreshToken);
            localStorage.setItem('expiration', data.expiration);

            return data.token;
        } else {
            throw new Error('토큰 갱신 실패');
        }
    } catch (error) {
        console.error('토큰 갱신 오류:', error);
        redirectToLogin();
        return null;
    }
}

// 로그인 페이지로 리다이렉트
function redirectToLogin() {
    localStorage.clear();
    window.location.href = '/login.html';
}

// 인증 확인 및 토큰 처리
async function checkAuthentication() {
    let token = localStorage.getItem('authToken');

    if (!token) {
        redirectToLogin();
        return null;
    }

    if (isTokenExpired()) {
        console.log('토큰이 만료되었습니다. 갱신을 시도합니다.');
        token = await refreshToken();
        if (!token) {
            return null;
        }
    }

    return token;
}

// 사용자 역할 확인
function checkUserRole() {
    const username = localStorage.getItem('username');
    const roles = localStorage.getItem('roles');

    isAdmin = username === 'admin' || (roles && roles.includes('Admin'));
    userRole = isAdmin ? 'Admin' : 'User';

    // UI 업데이트
    document.getElementById('roleDisplay').textContent = isAdmin ? '관리자' : '일반 사용자';

    if (isAdmin) {
        document.getElementById('adminControls').style.display = 'flex';
        document.getElementById('adminDashboard').classList.remove('hidden');
        document.getElementById('userDashboard').classList.add('hidden');
        loadAdminDashboard();
    } else {
        document.getElementById('adminControls').style.display = 'none';
        document.getElementById('adminDashboard').classList.add('hidden');
        document.getElementById('userDashboard').classList.remove('hidden');
        loadUserDashboard();
    }
}

// 관리자 대시보드 로드
async function loadAdminDashboard() {
    const token = await checkAuthentication();
    if (!token) return;

    try {
        // 사용자 통계
        const usersResponse = await fetch('/api/users?includeInactive=true', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (usersResponse.ok) {
            const users = await usersResponse.json();
            adminStats.users.total = users.length;
            adminStats.users.active = users.filter(u => u.active).length;
            adminStats.users.inactive = users.filter(u => !u.active).length;
        }

        // 회사 통계
        const companiesResponse = await fetch('/api/companies?includeInactive=true', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (companiesResponse.ok) {
            const companies = await companiesResponse.json();
            adminStats.companies.total = companies.length;
            adminStats.companies.active = companies.filter(c => c.active).length;
            adminStats.companies.inactive = companies.filter(c => !c.active).length;
        }

        // 센서 통계
        const sensorsResponse = await fetch('/api/sensors', {
            headers: { 'Authorization': `Bearer ${token}` }
        });
        if (sensorsResponse.ok) {
            const sensors = await sensorsResponse.json();
            adminStats.sensors.total = sensors.length;
            adminStats.sensors.online = sensors.filter(s => s.connectionStatus === 'online').length;
            adminStats.sensors.tempHumidity = sensors.filter(s => s.sensorType === 'temp_humidity').length;
            adminStats.sensors.particle = sensors.filter(s => s.sensorType === 'particle').length;
        }

        updateAdminUI();
        loadRecentActivities();

    } catch (error) {
        console.error('관리자 통계 로드 오류:', error);
    }
}

// 관리자 UI 업데이트
function updateAdminUI() {
    document.getElementById('adminTotalUsers').textContent = adminStats.users.total;
    document.getElementById('adminActiveUsers').textContent = adminStats.users.active;
    document.getElementById('adminInactiveUsers').textContent = adminStats.users.inactive;

    document.getElementById('adminTotalCompanies').textContent = adminStats.companies.total;
    document.getElementById('adminActiveCompanies').textContent = adminStats.companies.active;
    document.getElementById('adminInactiveCompanies').textContent = adminStats.companies.inactive;

    document.getElementById('adminTotalSensors').textContent = adminStats.sensors.total;
    document.getElementById('adminOnlineSensors').textContent = adminStats.sensors.online;
    document.getElementById('adminTempSensors').textContent = adminStats.sensors.tempHumidity;
    document.getElementById('adminParticleSensors').textContent = adminStats.sensors.particle;

    const connectionRate = adminStats.sensors.total > 0
        ? Math.round((adminStats.sensors.online / adminStats.sensors.total) * 100)
        : 0;
    document.getElementById('adminConnectionRate').textContent = connectionRate + '%';

    // 알림 통계 (예시)
    document.getElementById('adminActiveAlerts').textContent = '3';
    document.getElementById('adminTodayAlerts').textContent = '8';
    document.getElementById('adminWeekAlerts').textContent = '42';
}

// 최근 활동 로드 (예시)
function loadRecentActivities() {
    const activities = [
        { type: 'user', title: '새 사용자 "user123" 등록', time: '5분 전' },
        { type: 'sensor', title: '센서 "온습도-01" 온라인', time: '10분 전' },
        { type: 'alert', title: '회사 "ABC" 센서 오프라인 알림', time: '30분 전' },
        { type: 'user', title: '사용자 "test01" 로그인', time: '1시간 전' },
        { type: 'sensor', title: '새 센서 "미세먼지-05" 등록', time: '2시간 전' }
    ];

    const container = document.getElementById('recentActivities');
    container.innerHTML = activities.map(activity => `
                <div class="activity-item">
                    <div class="activity-icon ${activity.type}">
                        ${activity.type === 'user' ? '👤' : activity.type === 'sensor' ? '📡' : '🔔'}
                    </div>
                    <div class="activity-details">
                        <div class="activity-title">${activity.title}</div>
                        <div class="activity-time">${activity.time}</div>
                    </div>
                </div>
            `).join('');
}

// 일반 사용자 대시보드 로드
async function loadUserDashboard() {
    const token = await checkAuthentication();
    if (!token) return;

    // 차트 초기화
    initializeCharts();

    // 회사 목록 로드
    await loadCompanies();

    // SignalR 연결 시작
    await startSignalRConnection(token);
}

// 로그아웃 함수
async function logout() {
    try {
        const token = localStorage.getItem('authToken');
        if (token) {
            await fetch('/api/auth/logout', {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });
        }
    } catch (error) {
        console.error('로그아웃 오류:', error);
    } finally {
        if (connection) {
            await connection.stop();
        }

        // 인증 관련 키만 삭제 (저장된 계정 정보는 그대로 유지)
        localStorage.removeItem('authToken');
        localStorage.removeItem('refreshToken');
        window.location.href = '/login.html';
    }
}

// === 일반 사용자 함수들 ===

// 차트 초기화
function initializeCharts() {
    const temperatureCtx = document.getElementById('temperatureChart').getContext('2d');
    temperatureChart = new Chart(temperatureCtx, {
        type: 'line',
        data: {
            labels: [],
            datasets: [{
                label: '온도 (°C)',
                data: [],
                borderColor: '#e74c3c',
                backgroundColor: 'rgba(231, 76, 60, 0.1)',
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: false,
                    title: { display: true, text: '온도 (°C)' }
                }
            }
        }
    });

    const humidityCtx = document.getElementById('humidityChart').getContext('2d');
    humidityChart = new Chart(humidityCtx, {
        type: 'line',
        data: {
            labels: [],
            datasets: [{
                label: '습도 (%)',
                data: [],
                borderColor: '#3498db',
                backgroundColor: 'rgba(52, 152, 219, 0.1)',
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    title: { display: true, text: '습도 (%)' }
                }
            }
        }
    });

    const particleCtx = document.getElementById('particleChart').getContext('2d');
    particleChart = new Chart(particleCtx, {
        type: 'line',
        data: {
            labels: [],
            datasets: [
                {
                    label: 'PM1.0 (㎍/㎥)',
                    data: [],
                    borderColor: '#3498db',
                    backgroundColor: 'rgba(52, 152, 219, 0.1)',
                    tension: 0.4,
                    hidden: true
                },
                {
                    label: 'PM2.5 (㎍/㎥)',
                    data: [],
                    borderColor: '#e74c3c',
                    backgroundColor: 'rgba(231, 76, 60, 0.1)',
                    tension: 0.4
                },
                {
                    label: 'PM4.0 (㎍/㎥)',
                    data: [],
                    borderColor: '#f39c12',
                    backgroundColor: 'rgba(243, 156, 18, 0.1)',
                    tension: 0.4,
                    hidden: true
                },
                {
                    label: 'PM10.0 (㎍/㎥)',
                    data: [],
                    borderColor: '#9b59b6',
                    backgroundColor: 'rgba(155, 89, 182, 0.1)',
                    tension: 0.4
                },
                {
                    label: 'PM0.5 (개수/㎥)',
                    data: [],
                    borderColor: '#1abc9c',
                    backgroundColor: 'rgba(26, 188, 156, 0.1)',
                    tension: 0.4,
                    hidden: true
                },
                {
                    label: 'PM5.0 (개수/㎥)',
                    data: [],
                    borderColor: '#34495e',
                    backgroundColor: 'rgba(52, 73, 94, 0.1)',
                    tension: 0.4,
                    hidden: true
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    title: { display: true, text: '농도' }
                }
            },
            plugins: {
                legend: {
                    display: true,
                    position: 'top',
                    labels: {
                        usePointStyle: true,
                        padding: 10,
                        font: {
                            size: 11
                        }
                    }
                },
                tooltip: {
                    mode: 'index',
                    intersect: false
                }
            },
            interaction: {
                mode: 'nearest',
                axis: 'x',
                intersect: false
            }
        }
    });
}

// 연결 상태 업데이트
function updateConnectionStatus(message, isConnected) {
    const statusElement = document.getElementById('connectionStatus');
    const statusIndicator = document.getElementById('signalrStatus');

    statusElement.textContent = message;
    statusIndicator.className = `connection-status ${isConnected ? 'connected' : 'disconnected'}`;
}

// 회사 목록 로드
async function loadCompanies() {
    const token = await checkAuthentication();
    if (!token) return;

    try {
        const response = await fetch('/api/companies', {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.ok) {
            companies = await response.json();
            renderCompanies();
        } else {
            console.error('회사 목록 로드 실패');
        }
    } catch (error) {
        console.error('회사 목록 로드 오류:', error);
    }
}

// 회사 목록 렌더링
function renderCompanies() {
    const container = document.getElementById('companiesList');

    if (companies.length === 0) {
        container.innerHTML = '<div class="empty-state">등록된 회사가 없습니다.</div>';
        return;
    }

    container.innerHTML = '';
    companies.forEach(company => {
        const companyItem = document.createElement('div');
        companyItem.className = 'company-item';
        companyItem.onclick = () => selectCompany(company.companyId);

        companyItem.innerHTML = `
                    <div class="company-name">${company.companyName}</div>
                    <div class="company-info">
                        <span>${company.address || '주소 미입력'}</span>
                        <span class="sensor-count">${company.sensorCount || 0}개 센서</span>
                    </div>
                `;

        container.appendChild(companyItem);
    });
}

// 회사 선택
async function selectCompany(companyId) {
    selectedCompanyId = companyId;

    // 선택된 회사 하이라이트
    document.querySelectorAll('.company-item').forEach(item => {
        item.classList.remove('selected');
    });
    event.currentTarget.classList.add('selected');

    // 선택된 회사의 센서 로드
    await loadSensorsByCompany(companyId);

    // 타이틀 업데이트
    const company = companies.find(c => c.companyId === companyId);
    document.getElementById('sensorListTitle').textContent = `📊 ${company.companyName} 센서 상태`;
}

// 회사별 센서 로드
async function loadSensorsByCompany(companyId) {
    const token = await checkAuthentication();
    if (!token) return;

    try {
        const response = await fetch(`/api/sensors?companyId=${companyId}`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        if (response.ok) {
            const sensors = await response.json();
            renderSensors(sensors);
        } else {
            console.error('센서 목록 로드 실패');
        }
    } catch (error) {
        console.error('센서 목록 로드 오류:', error);
    }
}

// 센서 목록 렌더링
function renderSensors(sensors) {
    const container = document.getElementById('sensorList');

    if (sensors.length === 0) {
        container.innerHTML = '<div class="empty-state">등록된 센서가 없습니다.</div>';
        return;
    }

    container.innerHTML = '';
    sensors.forEach(sensor => {
        const typeInfo = getSensorTypeInfo(sensor.sensorType);
        const sensorItem = document.createElement('div');
        sensorItem.className = `sensor-item ${sensor.connectionStatus !== 'online' ? 'offline' : ''}`;

        let valueDisplay = '';
        if (sensor.latestData) {
            switch (sensor.sensorType) {
                case 'temp_humidity':
                    valueDisplay = `${sensor.latestData.temperature || '--'}°C, ${sensor.latestData.humidity || '--'}%`;
                    break;
                case 'particle':
                    valueDisplay = `PM2.5: ${sensor.latestData.pm2_5 || '--'} | PM10: ${sensor.latestData.pm10_0 || '--'}㎍/㎥`;
                    break;
                case 'wind':
                    valueDisplay = `풍속: ${sensor.latestData.windSpeed || '--'}m/s`;
                    break;
                case 'speaker':
                    valueDisplay = `${sensor.latestData.powerStatus ? '🔊 ON' : '🔇 OFF'}`;
                    break;
                default:
                    valueDisplay = '데이터 없음';
            }
        }

        sensorItem.innerHTML = `
                    <div class="sensor-info">
                        <div class="sensor-name">${sensor.name || `센서 ${sensor.sensorId}`}</div>
                        <div class="sensor-type">
                            <span class="sensor-type-icon">${typeInfo.icon}</span>
                            ${typeInfo.name}
                        </div>
                    </div>
                    <div class="sensor-values">
                        <div class="sensor-value">${valueDisplay}</div>
                        <div class="sensor-time">${sensor.lastCommunication ? new Date(sensor.lastCommunication).toLocaleTimeString('ko-KR') : '데이터 없음'}</div>
                    </div>
                `;

        container.appendChild(sensorItem);
    });
}

// 전체 센서 보기 페이지로 이동
function viewAllSensors() {
    window.location.href = '/sensor-groups.html';
}

// SignalR 연결 함수
async function startSignalRConnection(token) {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/sensorHub", {
            accessTokenFactory: () => token
        })
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();

    connection.on("SensorDataReceived", function (sensorData) {
        console.log("센서 데이터 수신:", sensorData);
        addRealtimeData(sensorData);
        updateCharts(sensorData);
    });

    try {
        await connection.start();
        updateConnectionStatus("SignalR 연결 성공!", true);
    } catch (error) {
        updateConnectionStatus("SignalR 연결 실패: " + error, false);
        console.error("SignalR 연결 실패:", error);
    }
}

// 차트 업데이트
function updateChart(chart, value, label) {
    chart.data.labels.push(label);
    chart.data.datasets[0].data.push(value);

    if (chart.data.labels.length > 20) {
        chart.data.labels.shift();
        chart.data.datasets[0].data.shift();
    }

    chart.update('none');
}

// 차트들 업데이트
function updateCharts(sensorData) {
    const time = new Date().toLocaleTimeString('ko-KR');

    if (sensorData.data.temperature !== undefined) {
        updateChart(temperatureChart, sensorData.data.temperature, time);
    }

    if (sensorData.data.humidity !== undefined) {
        updateChart(humidityChart, sensorData.data.humidity, time);
    }

    // 파티클 센서 데이터 처리
    if (sensorData.sensorType === 'particle') {
        const particleData = sensorData.data;

        // 모든 데이터셋에 동일한 시간 라벨 추가
        if (particleChart.data.labels.length === 0 ||
            particleChart.data.labels[particleChart.data.labels.length - 1] !== time) {
            particleChart.data.labels.push(time);

            // 20개 이상이면 가장 오래된 데이터 제거
            if (particleChart.data.labels.length > 20) {
                particleChart.data.labels.shift();
            }
        }

        // 각 PM 값 업데이트
        if (particleData.pm1_0 !== undefined) {
            updateParticleDataset(0, particleData.pm1_0);
        }
        if (particleData.pm2_5 !== undefined) {
            updateParticleDataset(1, particleData.pm2_5);
        }
        if (particleData.pm4_0 !== undefined) {
            updateParticleDataset(2, particleData.pm4_0);
        }
        if (particleData.pm10_0 !== undefined) {
            updateParticleDataset(3, particleData.pm10_0);
        }
        if (particleData.pm_0_5 !== undefined) {
            updateParticleDataset(4, particleData.pm_0_5);
        }
        if (particleData.pm_5_0 !== undefined) {
            updateParticleDataset(5, particleData.pm_5_0);
        }

        particleChart.update('none');
    }
}

// 파티클 데이터셋 업데이트 함수
function updateParticleDataset(datasetIndex, value) {
    particleChart.data.datasets[datasetIndex].data.push(value);

    // 20개 이상이면 가장 오래된 데이터 제거
    if (particleChart.data.datasets[datasetIndex].data.length > 20) {
        particleChart.data.datasets[datasetIndex].data.shift();
    }
}

// 실시간 데이터 추가
function addRealtimeData(sensorData) {
    const container = document.getElementById('realtimeData');

    if (container.querySelector('.empty-state')) {
        container.innerHTML = '';
    }

    const dataItem = document.createElement('div');
    dataItem.className = 'data-item';

    const typeInfo = getSensorTypeInfo(sensorData.sensorType);
    let values = `${typeInfo.icon} `;

    if (sensorData.data.temperature !== undefined) {
        values += `🌡️ ${sensorData.data.temperature}°C `;
    }
    if (sensorData.data.humidity !== undefined) {
        values += `💧 ${sensorData.data.humidity}% `;
    }

    // 파티클 센서 데이터 표시
    if (sensorData.sensorType === 'particle') {
        values = `${typeInfo.icon} `;
        if (sensorData.data.pm1_0 !== undefined) {
            values += `PM1.0: ${sensorData.data.pm1_0} `;
        }
        if (sensorData.data.pm2_5 !== undefined) {
            values += `PM2.5: ${sensorData.data.pm2_5} `;
        }
        if (sensorData.data.pm10_0 !== undefined) {
            values += `PM10: ${sensorData.data.pm10_0}㎍/㎥ `;
        }
    }

    if (sensorData.data.windSpeed !== undefined) {
        values += `🌪️ ${sensorData.data.windSpeed}m/s `;
    }

    dataItem.innerHTML = `
                <div><strong>${typeInfo.name} ${sensorData.sensorId}</strong></div>
                <div class="data-values">${values}</div>
                <div class="timestamp">${new Date().toLocaleString('ko-KR')}</div>
            `;

    container.insertBefore(dataItem, container.firstChild);

    while (container.children.length > 10) {
        container.removeChild(container.lastChild);
    }
}

// 유틸리티 함수들
function refreshCompanies() {
    loadCompanies();
}

function refreshSensors() {
    if (selectedCompanyId) {
        loadSensorsByCompany(selectedCompanyId);
    }
}

function clearSensorList() {
    document.getElementById('sensorList').innerHTML = '<div class="empty-state">회사를 선택하여 센서를 확인하세요</div>';
}

function clearRealtimeData() {
    const container = document.getElementById('realtimeData');
    container.innerHTML = '<div class="empty-state">실시간 데이터를 기다리는 중...</div>';
}

function exportData() {
    const data = Array.from(sensorStates.entries()).map(([id, sensor]) => ({
        sensorId: id,
        sensorUuid: sensor.sensorUuid,
        sensorType: sensor.sensorType,
        data: sensor.data,
        lastUpdate: sensor.lastUpdate
    }));

    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `sensor_data_${new Date().toISOString().slice(0, 10)}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

// 초기화 함수
async function initializeDashboard() {
    const token = await checkAuthentication();
    if (!token) return;

    // 사용자 정보 표시
    const fullName = localStorage.getItem('fullName');
    const username = localStorage.getItem('username');
    const lastLogin = localStorage.getItem('lastLogin');

    let welcomeText = `환영합니다, ${fullName || username}님!`;
    if (lastLogin) {
        const lastLoginDate = new Date(lastLogin).toLocaleString('ko-KR');
        welcomeText += ` (마지막 로그인: ${lastLoginDate})`;
    }

    document.getElementById('welcomeMessage').textContent = welcomeText;

    // 사용자 역할 확인 및 적절한 대시보드 로드
    checkUserRole();
}

// 페이지 로드 시 초기화
document.addEventListener('DOMContentLoaded', function () {
    initializeDashboard();
});

// 페이지 언로드 시 연결 정리
window.addEventListener('beforeunload', function () {
    if (connection) {
        connection.stop();
    }
});