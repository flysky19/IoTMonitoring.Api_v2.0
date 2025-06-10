// ===== Global Variables =====
let connection = null;
let companies = [];
let sensorGroups = [];
let sensors = [];
let selectedCompanyId = null;
let selectedGroupId = null;
let isEventLogPaused = false;
let currentViewMode = 'grid'; // 'grid' or 'list'
let selectedSensor = null;

// Components
let startDatePicker = null;
let endDatePicker = null;
let chartStartDatePicker = null;
let chartEndDatePicker = null;
let rawDataGrid = null;
let historyChart = null;

// 센서 타입 정보
const sensorTypeInfo = {
    'particle': { icon: '💨', name: '미세먼지 센서', unit: 'P-Counter' },
    'temp_humidity': { icon: '🌡️', name: '온습도 센서', unit: '°C / %' },
    'wind': { icon: '🌪️', name: '풍속 센서', unit: 'm/s' },
    'speaker': { icon: '🔊', name: '스피커', unit: '' }
};

// ===== Global Variables에 추가 =====
let pollingInterval = null;
let pollingIntervalTime = 5000; // 30초 (기본값)
let isPollingEnabled = true;
let lastPollingTime = null;

// ===== Initialize =====
document.addEventListener('DOMContentLoaded', async function () {
    console.log('Dashboard 초기화 시작');

    try {
        // 인증 확인
        const token = await checkAuthentication();
        if (!token) {
            console.log('인증 실패 - 로그인 페이지로 이동');
            sessionStorage.setItem('redirecting', 'true');
            window.location.replace('/login.html');
            return;
        }

        const licenseKey = await getSyncFusionLicense();
        if (licenseKey) {
            console.log('SyncFusion 라이센스 등록 완료');
            ej.base.registerLicense(licenseKey);
        }

        console.log('인증 성공');
        sessionStorage.removeItem('redirecting');

        // 사용자 정보 표시
        displayUserInfo();

        // 컴포넌트 초기화
        initializeComponents();

        // 이벤트 리스너 설정
        setupEventListeners();

        // 데이터 로드
        await loadInitialData();

        startDataPolling();

        // SignalR 연결
        await initializeSignalR(token);

        // 현재 시간 표시
        updateDateTime();
        setInterval(updateDateTime, 1000);

        console.log('Dashboard 초기화 완료');
    } catch (error) {
        console.error('초기화 중 오류:', error);
        showToast({
            message: '페이지 초기화 중 오류가 발생했습니다.',
            type: 'error'
        });
    }
});

/**
 * 데이터 폴링 시작
 */
function startDataPolling() {
    
    // 기존 폴링 중지
    stopDataPolling();
    
    // 즉시 한 번 실행
    pollSensorData();

    // 새 폴링 시작
    pollingInterval = setInterval(async () => {
        if (document.hidden) {
            // 브라우저 탭이 비활성 상태면 폴링 스킵
            return;
        }
        
        await pollSensorData();
    }, pollingIntervalTime);
    
    console.log(`데이터 폴링 시작 - 주기: ${pollingIntervalTime/1000}초`);
}

/**
 * 데이터 폴링 중지
 */
function stopDataPolling() {
    if (pollingInterval) {
        clearInterval(pollingInterval);
        pollingInterval = null;
        console.log('데이터 폴링 중지');
    }
}

/**
 * 센서 데이터 폴링
 */
async function pollSensorData() {
    try {
        const startTime = Date.now();
        console.log('센서 데이터 폴링 시작...');
        
        // 온라인 센서만 필터링
        const onlineSensors = sensors.filter(sensor => 
            sensor.connectionStatus === 'online'
        );
        
        if (onlineSensors.length === 0) {
            console.log('온라인 센서가 없습니다.');
            return;
        }
        
        console.log(`온라인 센서 ${onlineSensors.length}개 데이터 업데이트 중...`);
        
        // 각 온라인 센서의 최신 데이터 가져오기
        const updatePromises = onlineSensors.map(async (sensor) => {
            try {
                const response = await apiCall(
                    `/api/sensors/${sensor.sensorID}/data?limit=1`
                );
                
                if (response && response.ok) {
                    const data = await response.json();
                    if (data && data.length > 0) {
                        const latestData = data[0];
                        
                        // 데이터가 실제로 변경된 경우만 업데이트
                        if (hasDataChanged(sensor.latestData, latestData)) {
                            sensor.latestData = latestData;
                            sensor.lastCommunication = latestData.timestamp;
                            updateSensorCard(sensor);
                            
                            // 데이터 변경 이벤트 로그
                            addEventLog('data', 
                                `센서 데이터 업데이트됨`,
                                sensor.name || `센서 ${sensor.sensorID}`
                            );
                        }
                    }
                }
            } catch (error) {
                console.error(`센서 ${sensor.sensorID} 데이터 폴링 실패:`, error);
            }
        });
        
        await Promise.all(updatePromises);
        
        const endTime = Date.now();
        const duration = endTime - startTime;
        
        lastPollingTime = new Date();
        updatePollingStatus();

        updateSensorCounts();
        console.log(`폴링 완료 - 소요시간: ${duration}ms`);
        
    } catch (error) {
        console.error('센서 데이터 폴링 중 오류:', error);
        showToast({
            message: 'DB 데이터 업데이트 중 오류가 발생했습니다.',
            type: 'error'
        });
    }
}

// 센서 수 업데이트 함수
function updateSensorCounts() {
      const totalCount = sensors.length;
    const onlineCount = sensors.filter(s => s.connectionStatus === 'online').length;
    
    const totalCountElement = document.getElementById('totalSensorCount');
    if (totalCountElement) {
        totalCountElement.textContent = totalCount;
    }
    
    const onlineCountElement = document.getElementById('onlineSensorCount');
    if (onlineCountElement) {
        onlineCountElement.textContent = onlineCount;
    }
    
    const lastUpdateElement = document.getElementById('lastUpdateTime');
    if (lastUpdateElement) {
        lastUpdateElement.textContent = lastPollingTime ? lastPollingTime.toLocaleTimeString('ko-KR') : '-';
    }
}

// 센서 카드 업데이트 시 애니메이션 추가
function updateSensorCard(sensor) {
    const card = document.querySelector(`[data-sensor-id="${sensor.sensorID}"]`);
    if (card) {
        const newCard = createSensorCard(sensor);
        newCard.classList.add('data-updated');
        card.replaceWith(newCard);
        
        // 애니메이션 제거
        setTimeout(() => {
            newCard.classList.remove('data-updated');
        }, 1000);
    }
}

/**
 * 데이터 변경 여부 확인
 */
function hasDataChanged(oldData, newData) {
    if (!oldData || !newData) return true;
    
    // 타임스탬프는 제외하고 실제 측정값만 비교
    const oldValues = { ...oldData };
    const newValues = { ...newData };
    delete oldValues.timestamp;
    delete newValues.timestamp;
    
    return JSON.stringify(oldValues) !== JSON.stringify(newValues);
}

/**
 * 폴링 주기 변경
 */
function setPollingInterval(seconds) {
    pollingIntervalTime = seconds * 1000;
    
    // 폴링 재시작
    startDataPolling();
    
    showToast({
        message: `폴링 주기가 ${seconds}초로 변경되었습니다.`,
        type: 'info'
    });
}
/**
 * 폴링 토글
 */
function togglePolling() {
    isPollingEnabled = !isPollingEnabled;
    
    if (isPollingEnabled) {
        startDataPolling();
        showToast({
            message: 'DB 폴링이 활성화되었습니다.',
            type: 'success'
        });
    } else {
        stopDataPolling();
        showToast({
            message: 'DB 폴링이 비활성화되었습니다.',
            type: 'info'
        });
    }
}

// ===== Authentication & Logout =====
async function checkAuthentication() {
    try {
        const token = localStorage.getItem('authToken');
        let userInfo = {};

        console.log('=== Dashboard 인증 체크 ===');
        console.log('1. 토큰:', token ? `있음` : '없음');

        if (!token) {
            console.log('토큰이 없습니다');
            return null;
        }

        // userInfo 가져오기 시도
        const userInfoStr = localStorage.getItem('userInfo');

        if (userInfoStr && userInfoStr !== 'undefined') {
            // userInfo가 있으면 파싱
            try {
                userInfo = JSON.parse(userInfoStr);
                console.log('2. userInfo에서 파싱:', userInfo);
            } catch (e) {
                console.error('userInfo 파싱 오류:', e);
            }
        }

        // userInfo가 없거나 userId가 없으면 개별 필드에서 조합
        if (!userInfo.userId) {
            console.log('3. 개별 필드에서 userInfo 구성 시도');

            const userId = localStorage.getItem('userId');
            const username = localStorage.getItem('username');
            const fullName = localStorage.getItem('fullName');
            const expiration = localStorage.getItem('expiration');

            if (userId) {
                userInfo = {
                    userId: userId,
                    username: username || '',
                    fullName: fullName || '',
                    role: 'User',
                    expiration: expiration
                };

                // 통합된 userInfo 저장
                localStorage.setItem('userInfo', JSON.stringify(userInfo));
                console.log('4. 개별 필드에서 생성한 userInfo:', userInfo);
            }
        }

        // 최종 userId 체크
        const userId = userInfo.userId || userInfo.id;
        console.log('5. 최종 userId:', userId);

        if (!userId) {
            console.log('userId를 찾을 수 없습니다');
            return null;
        }

        // 토큰 만료 확인
        //if (userInfo.expiration) {
        //    const expirationDate = new Date(userInfo.expiration);
        //    const now = new Date();

        //    if (expirationDate < now) {
        //        console.log('토큰이 만료되었습니다');
        //        localStorage.clear();
        //        return null;
        //    }
        //}

        console.log('인증 성공!');
        return token;
    } catch (error) {
        console.error('인증 체크 중 오류:', error);
        return null;
    }
}

async function logout() {
    // 확인 다이얼로그
    const result = await Swal.fire({
        title: '로그아웃',
        text: '정말 로그아웃 하시겠습니까?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: '로그아웃',
        cancelButtonText: '취소',
        background: '#1e1e1e',
        color: '#fff'
    });

    if (!result.isConfirmed) {
        return;
    }

    showLoading(true);

    try {
        // SignalR 연결 종료
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            await connection.stop();
            console.log('SignalR 연결이 종료되었습니다.');
        }

        // 서버에 로그아웃 요청
        const token = localStorage.getItem('authToken');
        if (token) {
            try {
                await fetch('/api/auth/logout', {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json'
                    }
                });
            } catch (error) {
                console.error('로그아웃 API 호출 실패:', error);
            }
        }

        // 로컬 스토리지 정리
        localStorage.removeItem('authToken');
        localStorage.removeItem('userInfo');
        localStorage.removeItem('refreshToken');
        sessionStorage.clear();

        // 로그인 페이지로 리다이렉트
        showToast({
            message: '로그아웃되었습니다.',
            type: 'success',
            duration: 2000
        });

        setTimeout(() => {
            window.location.href = '/login.html';
        }, 1000);

    } catch (error) {
        console.error('로그아웃 처리 중 오류:', error);
        showToast({
            message: '로그아웃 처리 중 오류가 발생했습니다.',
            type: 'error'
        });

        setTimeout(() => {
            localStorage.clear();
            window.location.href = '/login.html';
        }, 2000);
    } finally {
        showLoading(false);
    }
}

function displayUserInfo() {
    const userInfo = JSON.parse(localStorage.getItem('userInfo') || '{}');
    const displayName = userInfo.fullName || userInfo.username || '사용자';
    const welcomeElement = document.getElementById('welcomeMessage');
    if (welcomeElement) {
        welcomeElement.textContent = `${displayName}님`;
    }
}

// ===== Components Initialization =====
function initializeComponents() {
    try {
        // Date Pickers
        if (typeof flatpickr !== 'undefined') {
            flatpickr.localize(flatpickr.l10ns.ko);

            const dateConfig = {
                dateFormat: "Y-m-d",
                maxDate: "today",
                locale: "ko",
                theme: "dark"
            };

            if (document.getElementById('startDate')) {
                startDatePicker = flatpickr("#startDate", dateConfig);
            }

            if (document.getElementById('endDate')) {
                endDatePicker = flatpickr("#endDate", dateConfig);
            }

            if (document.getElementById('chartStartDate')) {
                chartStartDatePicker = flatpickr("#chartStartDate", dateConfig);
            }

            if (document.getElementById('chartEndDate')) {
                chartEndDatePicker = flatpickr("#chartEndDate", dateConfig);
            }
        } else {
            console.error('flatpickr가 로드되지 않았습니다');
        }

        // Context Menu
        setupContextMenu();

        console.log('컴포넌트 초기화 완료');
    } catch (error) {
        console.error('컴포넌트 초기화 오류:', error);
    }
}

// ===== Event Listeners =====
function setupEventListeners() {
     // Logout
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', logout);
    }

    // Profile
    const profileBtn = document.getElementById('profileBtn');
    if (profileBtn) {
        profileBtn.addEventListener('click', () => {
            window.location.href = '/user-profile.html';
        });
    }

    // Refresh
    const refreshBtn = document.getElementById('refreshBtn');
    if (refreshBtn) {
        refreshBtn.addEventListener('click', refreshSensors);
    }

    // View Mode
    const gridViewBtn = document.getElementById('gridViewBtn');
    if (gridViewBtn) {
        gridViewBtn.addEventListener('click', () => setViewMode('grid'));
    }

    const listViewBtn = document.getElementById('listViewBtn');
    if (listViewBtn) {
        listViewBtn.addEventListener('click', () => setViewMode('list'));
    }

    // Event Log
    const clearLogBtn = document.getElementById('clearLogBtn');
    if (clearLogBtn) {
        clearLogBtn.addEventListener('click', clearEventLog);
    }

    const pauseLogBtn = document.getElementById('pauseLogBtn');
    if (pauseLogBtn) {
        pauseLogBtn.addEventListener('click', toggleEventLogPause);
    }

    // Date Range Buttons
    const loadDataBtn = document.getElementById('loadDataBtn');
    if (loadDataBtn) {
        loadDataBtn.addEventListener('click', loadRawData);
    }

    const loadChartBtn = document.getElementById('loadChartBtn');
    if (loadChartBtn) {
        loadChartBtn.addEventListener('click', loadHistoryChart);
    }

    // 폴링 관련 이벤트 리스너 (요소가 있는 경우에만)
    const togglePollingBtn = document.getElementById('togglePollingBtn');
    if (togglePollingBtn) {
        togglePollingBtn.addEventListener('click', togglePolling);
    }

    const pollingIntervalSelect = document.getElementById('pollingIntervalSelect');
    if (pollingIntervalSelect) {
        pollingIntervalSelect.addEventListener('change', (e) => {
            const seconds = parseInt(e.target.value);
            setPollingInterval(seconds);
        });
    }

    // 초기 상태 업데이트 (요소가 있는 경우에만)
    if (document.getElementById('pollingStatus')) {
        updatePollingStatus();
        // 상태 변경 시마다 UI 업데이트
        setInterval(updatePollingStatus, 1000);
    }

    // 페이지 떠날 때 처리
    window.addEventListener('beforeunload', async (e) => {
        // 폴링 중지
        stopDataPolling();

        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            await connection.stop();
        }
    });

    // 페이지 가시성 변경 감지 (탭 전환 시)
    document.addEventListener('visibilitychange', () => {
        if (document.hidden) {
            // 탭이 비활성화되면 폴링 중지
            stopDataPolling();
        } else {
            // 탭이 활성화되면 폴링 재시작
            if (typeof isPollingEnabled !== 'undefined' && isPollingEnabled) {
                startDataPolling();
            }
        }
    });
}

// ===== 선택적: 폴링 상태 표시 UI =====
function updatePollingStatus() {
    const statusElement = document.getElementById('pollingStatus');
    if (statusElement) {
        const onlineCount = sensors.filter(s => s.connectionStatus === 'online').length;
        const lastUpdate = lastPollingTime ? 
            `마지막 업데이트: ${lastPollingTime.toLocaleTimeString('ko-KR')}` : 
            '대기 중';
        
        statusElement.innerHTML = `
            <i class="fas fa-database"></i> 
            DB 폴링 (${pollingIntervalTime/1000}초) | 
            온라인: ${onlineCount}개 | 
            ${lastUpdate}
        `;
    }
}

async function getSyncFusionLicense() {
    try {
        const response = await apiCall('/api/config/syncfusion-license');
        const data = await response.json();
        return data.licenseKey;
    } catch (error) {
        console.error('라이센스 키 로드 실패:', error);
        return null;
    }
}

// ===== Data Loading =====
async function loadInitialData() {
    showLoading(true);

    try {
        await loadCompanies();
    } catch (error) {
        console.error('초기 데이터 로드 실패:', error);
        showToast({
            message: '데이터를 불러오는데 실패했습니다.',
            type: 'error'
        });
    } finally {
        showLoading(false);
    }
}

async function loadCompanies() {
    try {
        const response = await apiCall('/api/companies/users');
        if (response && response.ok) {
            companies = await response.json();
            console.log('회사 목록:', companies);

            renderCompanyList();

            // 첫 번째 회사 자동 선택
            if (companies.length > 0) {
                selectCompany(companies[0].companyID);
            }
        }
    } catch (error) {
        console.error('회사 목록 로드 실패:', error);
        showToast({
            message: '회사 목록을 불러오는데 실패했습니다.',
            type: 'error'
        });

        // 에러 상태 표시
        const container = document.getElementById('companyList');
        if (container) {
            container.innerHTML = `
                <div class="empty-company-list">
                    <i class="fas fa-exclamation-triangle"></i>
                    <p>회사 목록을 불러올 수 없습니다.</p>
                    <button class="btn btn-sm btn-primary" onclick="loadCompanies()">
                        다시 시도
                    </button>
                </div>
            `;
        }
    }
}

function renderCompanyList() {
    const container = document.getElementById('companyList');
    if (!container) return;

    if (companies.length === 0) {
        container.innerHTML = `
            <div class="empty-company-list">
                <i class="fas fa-building"></i>
                <p>등록된 회사가 없습니다.</p>
            </div>
        `;
        return;
    }

    container.innerHTML = '';

    companies.forEach(company => {
        const card = document.createElement('div');
        card.className = 'company-card';
        card.dataset.companyId = company.companyID;

        card.innerHTML = `
            <div class="company-name">
                <i class="fas fa-building"></i>
                ${company.companyName}
            </div>
            <div class="company-info">
                ${company.address ? `<span><i class="fas fa-map-marker-alt"></i> ${company.address}</span>` : ''}
                ${company.contactPerson ? `<span><i class="fas fa-user"></i> ${company.contactPerson}</span>` : ''}
            </div>
            <div class="company-stats">
                <div class="stat-item">
                    <i class="fas fa-microchip"></i>
                    <span class="stat-value">${company.sensorCount || 0}</span>
                    <span>센서</span>
                </div>
                <div class="stat-item">
                    <i class="fas fa-layer-group"></i>
                    <span class="stat-value">${company.groupCount || 0}</span>
                    <span>그룹</span>
                </div>
            </div>
        `;

        card.addEventListener('click', () => selectCompany(company.companyID));
        container.appendChild(card);
    });
}

function selectCompany(companyId) {
    // 이전 선택 제거
    document.querySelectorAll('.company-card').forEach(card => {
        card.classList.remove('selected');
    });

    // 새 선택 추가
    const selectedCard = document.querySelector(`[data-company-id="${companyId}"]`);
    if (selectedCard) {
        selectedCard.classList.add('selected');
    }

    selectedCompanyId = companyId;

    // 그룹 및 센서 로드
    loadSensorGroups(companyId);
    loadSensors();
}

function refreshCompanies() {
    const container = document.getElementById('companyList');
    if (container) {
        container.innerHTML = `
            <div class="loading-spinner">
                <i class="fas fa-spinner fa-spin"></i>
                <span>회사 목록을 새로고침하는 중...</span>
            </div>
        `;
    }

    loadCompanies();
}

async function loadSensorGroups(companyId) {
    try {
        const response = await apiCall(`/api/sensor-groups?companyId=${companyId}`);
        if (response && response.ok) {
            sensorGroups = await response.json();
            updateGroupTreeView();
        }
    } catch (error) {
        console.error('센서 그룹 로드 실패:', error);
        showToast({
            message: '센서 그룹을 불러오는데 실패했습니다.',
            type: 'error'
        });
    }
}

async function loadSensors(groupId = null) {
    try {
        let url = '/api/sensors';
        if (groupId) {
            url += `?groupId=${groupId}`;
        } else if (selectedCompanyId) {
            url += `?companyId=${selectedCompanyId}`;
        }

        const response = await apiCall(url);
        if (response && response.ok) {
            sensors = await response.json();
            renderSensors();
        }
    } catch (error) {
        console.error('센서 로드 실패:', error);
        showToast({
            message: '센서 목록을 불러오는데 실패했습니다.',
            type: 'error'
        });
    }
}

// ===== UI Updates =====
function updateGroupTreeView() {
    const container = document.getElementById('groupTreeView');
    container.innerHTML = '';

    if (sensorGroups.length === 0) {
        container.innerHTML = '<div class="tree-empty">등록된 그룹이 없습니다.</div>';
        return;
    }

    sensorGroups.forEach(group => {
        const item = document.createElement('div');
        item.className = 'tree-item';
        item.dataset.groupId = group.groupID;
        item.innerHTML = `
            <i class="fas fa-folder tree-item-icon"></i>
            <span class="tree-item-text">${group.groupName}</span>
            <span class="tree-item-count">${group.sensorCount || 0}</span>
        `;
        item.addEventListener('click', () => selectGroup(group.groupID));
        container.appendChild(item);
    });
}

function selectGroup(groupId) {
    // 이전 선택 제거
    document.querySelectorAll('.tree-item').forEach(item => {
        item.classList.remove('selected');
    });

    // 새 선택 추가
    const selectedItem = document.querySelector(`[data-group-id="${groupId}"]`);
    if (selectedItem) {
        selectedItem.classList.add('selected');
    }

    selectedGroupId = groupId;
    loadSensors(groupId);
}

function renderSensors() {
    const container = document.getElementById('sensorGrid');
    container.innerHTML = '';

    if (sensors.length === 0) {
        container.innerHTML = '<div class="empty-state">등록된 센서가 없습니다.</div>';
        return;
    }

    container.className = currentViewMode === 'list' ? 'sensor-list' : 'sensor-grid';

    sensors.forEach(sensor => {
        const card = createSensorCard(sensor);
        container.appendChild(card);
    });
}

function createSensorCard(sensor) {
    const typeInfo = sensorTypeInfo[sensor.sensorType] || { icon: '📡', name: '일반 센서' };
    const isOnline = sensor.connectionStatus === 'online';

    const card = document.createElement('div');
    card.className = `sensor-card ${!isOnline ? 'offline' : ''}`;
    card.dataset.sensorId = sensor.sensorID;

    let dataHtml = '';
    if (sensor.latestData) {
        switch (sensor.sensorType) {
            case 'temp_humidity':
                dataHtml = `
                    <div class="data-item">
                        <div class="data-label">온도</div>
                        <div class="data-value">${sensor.latestData.temperature || '--'}°C</div>
                    </div>
                    <div class="data-item">
                        <div class="data-label">습도</div>
                        <div class="data-value">${sensor.latestData.humidity || '--'}%</div>
                    </div>
                `;
                break;
            case 'particle':
                  // 디버깅을 위한 상세 로그
                    console.log('Particle sensor data:', sensor.latestData);
                    if (sensor.latestData) {
                        console.log('Available fields:', Object.keys(sensor.latestData));
                    }
    
                    // 대소문자 구분 없이 값 가져오기
                    const getValue = (data, fieldNames) => {
                        if (!data) return '--';
                        for (const field of fieldNames) {
                            if (data[field] !== undefined && data[field] !== null) {
                                return data[field];
                            }
                        }
                        return '--';
                    };
    
                    dataHtml = `
                        <div class="data-item">
                            <div class="data-label">PM0.3</div>
                            <div class="data-value ${getPMLevel(getValue(sensor.latestData, ['pm0_3', 'PM0_3', 'pM0_3']))}">${getValue(sensor.latestData, ['pm0_3', 'PM0_3', 'pM0_3'])}</div>
                        </div>
                        <div class="data-item">
                            <div class="data-label">PM0.5</div>
                            <div class="data-value ${getPMLevel(getValue(sensor.latestData, ['pm0_5', 'PM0_5', 'pM0_5']))}">${getValue(sensor.latestData, ['pm0_5', 'PM0_5', 'pM0_5'])}</div>
                        </div>
                        <div class="data-item">
                            <div class="data-label">PM1.0</div>
                            <div class="data-value ${getPMLevel(getValue(sensor.latestData, ['pm1_0', 'PM1_0', 'pM1_0']))}">${getValue(sensor.latestData, ['pm1_0', 'PM1_0', 'pM1_0'])}</div>
                        </div>
                        <div class="data-item">
                            <div class="data-label">PM2.5</div>
                            <div class="data-value ${getPMLevel(getValue(sensor.latestData, ['pm2_5', 'PM2_5', 'pM2_5']))}">${getValue(sensor.latestData, ['pm2_5', 'PM2_5', 'pM2_5'])}</div>
                        </div>
                        <div class="data-item">
                            <div class="data-label">PM5.0</div>
                            <div class="data-value ${getPMLevel(getValue(sensor.latestData, ['pm5_0', 'PM5_0', 'pM5_0']))}">${getValue(sensor.latestData, ['pm5_0', 'PM5_0', 'pM5_0'])}</div>
                        </div>
                        <div class="data-item">
                            <div class="data-label">PM10</div>
                            <div class="data-value ${getPMLevel(getValue(sensor.latestData, ['pm10', 'PM10', 'pM10']))}">${getValue(sensor.latestData, ['pm10', 'PM10', 'pM10'])}</div>
                        </div>
                    `;
                    break;
            case 'wind':
                dataHtml = `
                    <div class="data-item">
                        <div class="data-label">풍속</div>
                        <div class="data-value">${sensor.latestData.windSpeed || '--'} m/s</div>
                    </div>
                `;
                break;
            case 'speaker':
                dataHtml = `
                    <div class="data-item">
                        <div class="data-label">상태</div>
                        <div class="data-value">${sensor.latestData.powerStatus ? '🔊 ON' : '🔇 OFF'}</div>
                    </div>
                    <div class="data-item">
                        <div class="data-label">볼륨</div>
                        <div class="data-value">${sensor.latestData.volume || '--'}</div>
                    </div>
                `;
                break;
        }
    }

    card.innerHTML = `
        <div class="sensor-header">
            <div class="sensor-info">
                <div class="sensor-name">${sensor.name || `센서 ${sensor.sensorID}`}</div>
                <div class="sensor-type">
                    <span class="sensor-type-icon">${typeInfo.icon}</span>
                    ${typeInfo.name}
                </div>
            </div>
            <div class="sensor-status">
                <span class="status-indicator ${!isOnline ? 'offline' : ''}"></span>
                <span>${isOnline ? '온라인' : '오프라인'}</span>
            </div>
        </div>
        <div class="sensor-data">
            ${dataHtml || '<div class="no-data">데이터 없음</div>'}
        </div>
        <div class="last-update">
            <span>마지막 업데이트</span>
            <span>${formatDateTime(sensor.lastCommunication)}</span>
        </div>
    `;

    card.addEventListener('click', (e) => {
        if (!e.target.closest('.sensor-actions')) {
            selectedSensor = sensor;
        }
    });

    return card;
}

// ===== Modal Functions =====
function showRawDataModal() {
    if (!selectedSensor) return;

    const modal = document.getElementById('rawDataModal');
    modal.style.display = 'flex';

    // 날짜 초기화 (최근 7일)
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 7);

    if (startDatePicker) startDatePicker.setDate(startDate);
    if (endDatePicker) endDatePicker.setDate(endDate);
}

function closeRawDataModal() {
    document.getElementById('rawDataModal').style.display = 'none';
}

function showHistoryChartModal() {
    if (!selectedSensor) return;

    const modal = document.getElementById('historyChartModal');
    modal.style.display = 'flex';

    // 날짜 초기화
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 7);

    if (chartStartDatePicker) chartStartDatePicker.setDate(startDate);
    if (chartEndDatePicker) chartEndDatePicker.setDate(endDate);
}

function closeHistoryChartModal() {
    document.getElementById('historyChartModal').style.display = 'none';
}

async function loadRawData() {
    if (!selectedSensor || !startDatePicker || !endDatePicker) return;

    const startDate = startDatePicker.selectedDates[0];
    const endDate = endDatePicker.selectedDates[0];

    if (!startDate || !endDate) {
        showToast({
            message: '날짜를 선택해주세요.',
            type: 'warning'
        });
        return;
    }

    showLoading(true);

    try {
        const response = await apiCall(
            `/api/sensors/${selectedSensor.sensorID}/data?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`
        );

        if (response && response.ok) {
            const data = await response.json();
            displayRawDataGrid(data);
        }
    } catch (error) {
        console.error('로우 데이터 로드 실패:', error);
        showToast({
            message: '데이터를 불러오는데 실패했습니다.',
            type: 'error'
        });
    } finally {
        showLoading(false);
    }
}

function displayRawDataGrid(data) {
    const gridDiv = document.getElementById('rawDataGrid');

    // SyncFusion Grid 컬럼 설정
    const columns = [
        {
            field: 'timestamp',
            headerText: '시간',
            width: 180,
            format: 'yMd HH:mm:ss',
            type: 'datetime'
        }
    ];

    // 센서 타입에 따른 컬럼 추가
    switch (selectedSensor.sensorType) {
        case 'temp_humidity':
            columns.push(
                { field: 'temperature', headerText: '온도 (°C)', width: 120, format: 'N2' },
                { field: 'humidity', headerText: '습도 (%)', width: 120, format: 'N2' }
            );
            break;
        case 'particle':
            columns.push(
                { field: 'pm0_3', headerText: 'PM0.3', width: 100, format: 'N2' },
                { field: 'pm0_5', headerText: 'PM0.5', width: 100, format: 'N2' },
                { field: 'pm1_0', headerText: 'PM1.0', width: 100, format: 'N2' },
                { field: 'pm2_5', headerText: 'PM2.5', width: 100, format: 'N2' },
                { field: 'pm5_0', headerText: 'PM5.0', width: 100, format: 'N2' },
                { field: 'pm10', headerText: 'PM10', width: 100, format: 'N2' }
            );
            break;
        case 'wind':
            columns.push(
                { field: 'windSpeed', headerText: '풍속 (m/s)', width: 120, format: 'N2' }
            );
            break;
    }

    // 날짜 데이터 형식 변환
    const formattedData = data.map(item => ({
        ...item,
        timestamp: new Date(item.timestamp)
    }));

    // 기존 그리드 제거
    if (rawDataGrid) {
        rawDataGrid.destroy();
    }
    gridDiv.innerHTML = '';

    // SyncFusion Grid 생성
    rawDataGrid = new ej.grids.Grid({
        dataSource: formattedData,
        columns: columns,
        allowPaging: true,
        pageSettings: { pageSize: 50 },
        allowSorting: true,
        allowFiltering: true,
        filterSettings: { type: 'Menu' },
        allowExcelExport: true,
        allowPdfExport: true,
        toolbar: ['ExcelExport', 'PdfExport', 'CsvExport', 'Search'],
        height: 500,
        theme: 'material-dark',
        toolbarClick: function (args) {
            if (args.item.id === 'rawDataGrid_excelexport') {
                rawDataGrid.excelExport();
            } else if (args.item.id === 'rawDataGrid_pdfexport') {
                rawDataGrid.pdfExport();
            } else if (args.item.id === 'rawDataGrid_csvexport') {
                rawDataGrid.csvExport();
            }
        }
    });

    rawDataGrid.appendTo(gridDiv);
}

function exportRawData() {
    if (rawDataGrid) {
        // CSV 내보내기
        rawDataGrid.csvExport();
    }
}

async function loadHistoryChart() {
    if (!selectedSensor || !chartStartDatePicker || !chartEndDatePicker) return;

    const startDate = chartStartDatePicker.selectedDates[0];
    const endDate = chartEndDatePicker.selectedDates[0];

    if (!startDate || !endDate) {
        showToast({
            message: '날짜를 선택해주세요.',
            type: 'warning'
        });
        return;
    }

    showLoading(true);

    try {
        const response = await apiCall(
            `/api/sensors/${selectedSensor.sensorID}/data?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`
        );

        if (response && response.ok) {
            const data = await response.json();
            displayHistoryChart(data);
        }
    } catch (error) {
        console.error('차트 데이터 로드 실패:', error);
        showToast({
            message: '차트 데이터를 불러오는데 실패했습니다.',
            type: 'error'
        });
    } finally {
        showLoading(false);
    }
}

function displayHistoryChart(data) {
    const ctx = document.getElementById('historyChart').getContext('2d');

    // 기존 차트 제거
    if (historyChart) {
        historyChart.destroy();
    }

    // 차트 데이터 준비
    const labels = data.map(item => new Date(item.timestamp).toLocaleString('ko-KR'));
    const datasets = [];

    // 센서 타입에 따른 데이터셋 생성
    switch (selectedSensor.sensorType) {
        case 'temp_humidity':
            datasets.push({
                label: '온도 (°C)',
                data: data.map(item => item.temperature),
                borderColor: 'rgb(255, 99, 132)',
                backgroundColor: 'rgba(255, 99, 132, 0.1)',
                yAxisID: 'y-temperature'
            });
            datasets.push({
                label: '습도 (%)',
                data: data.map(item => item.humidity),
                borderColor: 'rgb(54, 162, 235)',
                backgroundColor: 'rgba(54, 162, 235, 0.1)',
                yAxisID: 'y-humidity'
            });
            break;
        case 'particle':
            datasets.push({
                label: 'PM0.3',
                data: data.map(item => item.pm0_3),
                borderColor: 'rgb(255, 206, 86)',
                backgroundColor: 'rgba(255, 206, 86, 0.1)'
            });
            datasets.push({
                label: 'PM0.5',
                data: data.map(item => item.pm0_5),
                borderColor: 'rgb(75, 192, 192)',
                backgroundColor: 'rgba(75, 192, 192, 0.1)'
            });
            datasets.push({
                label: 'PM1.0',
                data: data.map(item => item.pm1_0),
                borderColor: 'rgb(153, 102, 255)',
                backgroundColor: 'rgba(153, 102, 255, 0.1)'
            });
            datasets.push({
                label: 'PM2.5',
                data: data.map(item => item.pm2_5),
                borderColor: 'rgb(255, 99, 132)',
                backgroundColor: 'rgba(255, 99, 132, 0.1)'
            });
            datasets.push({
                label: 'PM5.0',
                data: data.map(item => item.pm5_0),
                borderColor: 'rgb(54, 162, 235)',
                backgroundColor: 'rgba(54, 162, 235, 0.1)'
            });
            datasets.push({
                label: 'PM10',
                data: data.map(item => item.pm10),
                borderColor: 'rgb(255, 159, 64)',
                backgroundColor: 'rgba(255, 159, 64, 0.1)'
            });
            break;
        case 'wind':
            datasets.push({
                label: '풍속 (m/s)',
                data: data.map(item => item.windSpeed),
                borderColor: 'rgb(153, 102, 255)',
                backgroundColor: 'rgba(153, 102, 255, 0.1)'
            });
            break;
    }

    // 차트 옵션 설정
    const options = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            title: {
                display: true,
                text: `${selectedSensor.name || '센서'} 히스토리`,
                color: '#fff',
                font: {
                    size: 16
                }
            },
            legend: {
                labels: {
                    color: '#fff'
                }
            }
        },
        scales: {
            x: {
                ticks: {
                    color: '#fff'
                },
                grid: {
                    color: 'rgba(255, 255, 255, 0.1)'
                }
            }
        }
    };

    // Y축 설정 (센서 타입별)
    if (selectedSensor.sensorType === 'temp_humidity') {
        options.scales['y-temperature'] = {
            type: 'linear',
            display: true,
            position: 'left',
            ticks: { color: '#fff' },
            grid: { color: 'rgba(255, 255, 255, 0.1)' }
        };
        options.scales['y-humidity'] = {
            type: 'linear',
            display: true,
            position: 'right',
            ticks: { color: '#fff' },
            grid: { drawOnChartArea: false }
        };
    } else {
        options.scales.y = {
            ticks: { color: '#fff' },
            grid: { color: 'rgba(255, 255, 255, 0.1)' }
        };
    }

    // 차트 생성
    historyChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: datasets
        },
        options: options
    });
}

function showSensorDetails() {
    if (!selectedSensor) return;

    Swal.fire({
        title: '센서 상세 정보',
        html: `
            <div style="text-align: left;">
                <p><strong>센서 ID:</strong> ${selectedSensor.sensorID}</p>
                <p><strong>센서 이름:</strong> ${selectedSensor.name || 'N/A'}</p>
                <p><strong>센서 타입:</strong> ${sensorTypeInfo[selectedSensor.sensorType]?.name || selectedSensor.sensorType}</p>
                <p><strong>UUID:</strong> ${selectedSensor.sensorUUID || 'N/A'}</p>
                <p><strong>상태:</strong> ${selectedSensor.connectionStatus}</p>
                <p><strong>설치일:</strong> ${formatDateTime(selectedSensor.installationDate)}</p>
            </div>
        `,
        icon: 'info',
        confirmButtonText: '확인',
        background: '#1e1e1e',
        color: '#fff'
    });
}

// ===== SignalR =====
async function initializeSignalR(token) {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/sensorHub", {
            accessTokenFactory: () => token
        })
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();

    // Event handlers
    //connection.on("SensorDataReceived", onSensorDataReceived);
    connection.on("SensorStatusChanged", onSensorStatusChanged);
    connection.on("AlertTriggered", onAlertTriggered);
    //connection.on("HeartbeatReceived", onHeartbeatTriggered);

    // Connection state handlers
    connection.onreconnecting(() => {
        showToast({
            message: '서버와 재연결 중...',
            type: 'warning'
        });

        updateConnectionStatus('reconnecting');
    });

    connection.onreconnected(() => {
         showToast({
                message: '서버와 재연결되었습니다. 실시간 모드로 전환합니다.',
                type: 'success'
            });
   
        updateConnectionStatus('connected');
    });

    connection.onclose(() => {
        showToast({
        message: '서버와의 연결이 끊어졌습니다. DB 폴링 모드로 전환합니다.',
        type: 'error'
        });
    
        updateConnectionStatus('disconnected');
    });

    try {
        await connection.start();
        showToast({
            message: '실시간 모니터링이 시작되었습니다.',
            type: 'success'
        });

        updateConnectionStatus('connected');

    } catch (error) {
        console.error('SignalR 연결 실패:', error);
        showToast({
            message: '실시간 연결에 실패했습니다.',
            type: 'error'
        });
        updateConnectionStatus('disconnected');
    }
}

/**
 * 연결 상태 업데이트
 */
function updateConnectionStatus(status) {
   const statusElement = document.getElementById('connectionStatus');
    if (statusElement) {
        switch(status) {
            case 'connected':
                statusElement.innerHTML = '<i class="fas fa-check-circle"></i> 서버 연결됨';
                statusElement.className = 'connection-status connected';
                break;
            case 'reconnecting':
                statusElement.innerHTML = '<i class="fas fa-sync-alt fa-spin"></i> 재연결 중...';
                statusElement.className = 'connection-status reconnecting';
                break;
            case 'disconnected':
                statusElement.innerHTML = '<i class="fas fa-exclamation-circle"></i> 연결 끊김';
                statusElement.className = 'connection-status disconnected';
                break;
        }
    }
}

function onSensorDataReceived(data) {
    const sensor = sensors.find(s => s.sensorID === data.sensorId);
    if (sensor) {
        sensor.latestData = data.data;
        sensor.lastCommunication = new Date();
        updateSensorCard(sensor);
        addEventLog('data',
            `센서 되었습니다.`,
            sensor.name
        );
    }
}

function onSensorStatusChanged(data) {
     const sensor = sensors.find(s => s.sensorID === data.sensorId);
    if (sensor) {
        const previousStatus = sensor.connectionStatus;
        sensor.connectionStatus = data.status;
        
        // 상태 변경 시 UI 업데이트
        updateSensorCard(sensor);
        
        // 온라인으로 변경된 경우 즉시 데이터 가져오기
        if (previousStatus !== 'online' && data.status === 'online') {
            // 개별 센서 데이터 즉시 업데이트
            updateSingleSensorData(sensor.sensorID);
        }
        
        addEventLog('connection',
            `센서가 ${data.status === 'online' ? '연결' : '연결 해제'}되었습니다.`,
            sensor.name || `센서 ${sensor.sensorID}`
        );
    }
}

/**
 * 단일 센서 데이터 업데이트
 */
async function updateSingleSensorData(sensorId) {
    try {
        const response = await apiCall(`/api/sensors/${sensorId}/data?limit=1`);
        
        if (response && response.ok) {
            const data = await response.json();
            if (data && data.length > 0) {
                const sensor = sensors.find(s => s.sensorID === sensorId);
                if (sensor) {
                    sensor.latestData = data[0];
                    sensor.lastCommunication = data[0].timestamp;
                    updateSensorCard(sensor);
                }
            }
        }
    } catch (error) {
        console.error(`센서 ${sensorId} 데이터 업데이트 실패:`, error);
    }
}

function onHeartbeatTriggered(data) {
   const sensor = sensors.find(s => s.sensorID === data.sensorId);
    if (sensor) {
        sensor.latestData = data.data;
        sensor.lastCommunication = new Date();
        updateSensorCard(sensor);
    }
}

function onAlertTriggered(data) {
    showToast({
        message: data.message,
        type: 'warning',
        duration: 10000
    });
    addEventLog('alert', data.message, data.sensorName);
}

// ===== Event Log =====
function addEventLog(type, message, sensorName = '') {
    if (isEventLogPaused) return;

    const logContainer = document.getElementById('eventLogContent');
    const eventItem = document.createElement('div');
    eventItem.className = 'event-item';

    const now = new Date();
    const timeString = now.toLocaleTimeString('ko-KR');

    eventItem.innerHTML = `
        <span class="event-time">${timeString}</span>
        <span class="event-type ${type}">
            <span class="event-type-icon"></span>
            ${type === 'connection' ? '연결' : type === 'alert' ? '알림' : '시스템'}
        </span>
        <span class="event-message">
            ${sensorName ? `<span class="event-sensor">[${sensorName}]</span> ` : ''}
            ${message}
        </span>
    `;

    logContainer.insertBefore(eventItem, logContainer.firstChild);

    // 최대 100개 유지
    while (logContainer.children.length > 100) {
        logContainer.removeChild(logContainer.lastChild);
    }
}

function clearEventLog() {
    document.getElementById('eventLogContent').innerHTML = '';
    addEventLog('system', '이벤트 로그가 초기화되었습니다.');
}

function toggleEventLogPause() {
    isEventLogPaused = !isEventLogPaused;
    const btn = document.getElementById('pauseLogBtn');
    btn.innerHTML = isEventLogPaused
        ? '<i class="fas fa-play"></i> 재개'
        : '<i class="fas fa-pause"></i> 일시정지';
}

// ===== Context Menu =====
function setupContextMenu() {
    // 간단한 컨텍스트 메뉴 구현
    document.addEventListener('contextmenu', function (e) {
        if (e.target.closest('.sensor-card')) {
            e.preventDefault();
            const sensorCard = e.target.closest('.sensor-card');
            const sensorId = sensorCard.dataset.sensorId;
            selectedSensor = sensors.find(s => s.sensorID == sensorId);

            const menu = document.getElementById('contextMenu');
            if (menu) {
                menu.style.display = 'block';
                menu.style.left = e.pageX + 'px';
                menu.style.top = e.pageY + 'px';
            }
        }
    });

    // 메뉴 외부 클릭 시 숨기기
    document.addEventListener('click', function () {
        const menu = document.getElementById('contextMenu');
        if (menu) {
            menu.style.display = 'none';
        }
    });

    // 메뉴 아이템 클릭 이벤트
    const menuItems = {
        'viewRawData': showRawDataModal,
        'viewChart': showHistoryChartModal,
        'viewDetails': showSensorDetails
    };

    Object.keys(menuItems).forEach(id => {
        const element = document.getElementById(id);
        if (element) {
            element.addEventListener('click', menuItems[id]);
        }
    });
}

// ===== Utility Functions =====
async function apiCall(url, options = {}) {
    const token = localStorage.getItem('authToken');
    const defaultOptions = {
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
            ...options.headers
        }
    };

    try {
        const response = await fetch(url, { ...options, ...defaultOptions });

        // 401 Unauthorized 처리
        if (response.status === 401) {
            showToast({
                message: '인증이 만료되었습니다. 다시 로그인해주세요.',
                type: 'warning'
            });
            setTimeout(() => {
                localStorage.clear();
                window.location.href = '/login.html';
            }, 2000);
            return null;
        }

        return response;
    } catch (error) {
        console.error('API 호출 오류:', error);
        throw error;
    }
}

function showLoading(show) {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.style.display = show ? 'flex' : 'none';
    }
}

// ===== Toast Notifications =====
function showToast(options) {
    // Toastify가 로드되었는지 확인
    if (typeof Toastify === 'undefined') {
        console.error('Toastify가 로드되지 않았습니다');
        // 대체 알림
        alert(options.message || '알림');
        return;
    }

    const defaultOptions = {
        text: options.message || '',
        duration: options.duration || 3000,
        gravity: "top",
        position: "right",
        stopOnFocus: true,
        close: true,
        style: {
            borderRadius: "4px",
            fontSize: "14px"
        }
    };

    // 타입별 스타일 설정
    switch (options.type) {
        case 'success':
            defaultOptions.style.background = "linear-gradient(to right, #00b09b, #96c93d)";
            break;
        case 'error':
            defaultOptions.style.background = "linear-gradient(to right, #ff5f6d, #ff3838)";
            break;
        case 'warning':
            defaultOptions.style.background = "linear-gradient(to right, #ff9800, #ff6f00)";
            break;
        case 'info':
        default:
            defaultOptions.style.background = "linear-gradient(to right, #2196F3, #0d7aff)";
            break;
    }

    Toastify(defaultOptions).showToast();
}

function updateDateTime() {
    const now = new Date();
    const dateTimeString = now.toLocaleString('ko-KR', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit'
    });

    const element = document.getElementById('currentDateTime');
    if (element) {
        element.textContent = dateTimeString;
    }
}

function formatDateTime(date) {
    if (!date) return '없음';
    const d = new Date(date);
    return d.toLocaleString('ko-KR');
}

function getPMLevel(value) {
    if (!value) return '';
    if (value <= 30) return '';
    if (value <= 80) return 'warning';
    return 'danger';
}

function setViewMode(mode) {
    currentViewMode = mode;
    document.getElementById('gridViewBtn').classList.toggle('active', mode === 'grid');
    document.getElementById('listViewBtn').classList.toggle('active', mode === 'list');
    renderSensors();
}

async function refreshSensors() {
    showLoading(true);
    try {
        // 센서 목록 새로고침
        if (selectedGroupId) {
            await loadSensors(selectedGroupId);
        } else if (selectedCompanyId) {
            await loadSensors();
        }
        
        // 온라인 센서 데이터 즉시 업데이트
        await pollSensorData();
        
        showToast({
            message: '센서 목록이 새로고침되었습니다.',
            type: 'success'
        });
    } catch (error) {
        showToast({
            message: '새로고침에 실패했습니다.',
            type: 'error'
        });
    } finally {
        showLoading(false);
    }
}

function updateSensorCard(sensor) {
    const card = document.querySelector(`[data-sensor-id="${sensor.sensorID}"]`);
    if (card) {
        const newCard = createSensorCard(sensor);
        card.replaceWith(newCard);
    }
}

// ===== System Log =====
window.addEventListener('load', () => {
    addEventLog('system', '시스템이 시작되었습니다.');
});
