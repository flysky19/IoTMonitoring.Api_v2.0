// ===== Sensor Detail Modal Management =====
let detailModalSensor = null;
let realtimeChart = null;
let historyDataGrid = null;
let realtimeUpdateInterval = null;
let isRealtimePaused = false;
let realtimeDataBuffer = [];
let historyStartPicker = null;
let historyEndPicker = null;

// HTML 로드 함수
async function loadSensorDetailModalHTML() {
    // 바로 HTML 삽입 (파일 로드 시도 제거)
    insertSensorDetailModalHTML();
}

// 대체 HTML 삽입 함수
function insertSensorDetailModalHTML() {
    const modalHTML = `
    <!-- Sensor Detail Modal -->
    <div id="sensorDetailModal" class="sensor-detail-modal" style="display: none;">
        <div class="sensor-detail-dialog">
            <div class="sensor-detail-header">
                <h2 class="sensor-detail-title">
                    <span id="sensorDetailIcon">📡</span>
                    <span id="sensorDetailName">센서 상세 정보</span>
                </h2>
                <button class="modal-close" onclick="closeSensorDetailModal()">
                    <i class="fas fa-times"></i>
                </button>
            </div>
            
            <div class="sensor-detail-body">
                <!-- 좌측: 센서 정보 패널 -->
                <div class="sensor-info-panel">
                    <div class="info-section">
                        <h3 class="info-section-title">
                            <i class="fas fa-info-circle"></i>
                            기본 정보
                        </h3>
                        <div class="info-grid">
                            <div class="info-item">
                                <span class="info-label">센서 ID</span>
                                <span class="info-value" id="detailSensorId">-</span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">센서 타입</span>
                                <span class="info-value" id="detailSensorType">-</span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">UUID</span>
                                <span class="info-value" id="detailSensorUUID">-</span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">모델</span>
                                <span class="info-value" id="detailSensorModel">-</span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">펌웨어 버전</span>
                                <span class="info-value" id="detailFirmwareVersion">-</span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">설치일</span>
                                <span class="info-value" id="detailInstallDate">-</span>
                            </div>
                        </div>
                    </div>
                    
                    <div class="info-section">
                        <h3 class="info-section-title">
                            <i class="fas fa-network-wired"></i>
                            연결 정보
                        </h3>
                        <div class="info-grid">
                            <div class="info-item">
                                <span class="info-label">연결 상태</span>
                                <span class="info-value" id="detailConnectionStatus">
                                    <span class="status-indicator"></span>
                                    <span class="status-text">-</span>
                                </span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">마지막 통신</span>
                                <span class="info-value" id="detailLastComm">-</span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">하트비트 주기</span>
                                <span class="info-value" id="detailHeartbeatInterval">-</span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">타임아웃 설정</span>
                                <span class="info-value" id="detailTimeout">-</span>
                            </div>
                        </div>
                    </div>
                    
                    <div class="info-section">
                        <h3 class="info-section-title">
                            <i class="fas fa-sitemap"></i>
                            소속 정보
                        </h3>
                        <div class="info-grid">
                            <div class="info-item">
                                <span class="info-label">회사</span>
                                <span class="info-value" id="detailCompany">-</span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">그룹</span>
                                <span class="info-value" id="detailGroup">-</span>
                            </div>
                            <div class="info-item">
                                <span class="info-label">위치</span>
                                <span class="info-value" id="detailLocation">-</span>
                            </div>
                        </div>
                    </div>
                    
                    <!-- 센서 타입별 추가 정보 -->
                    <div class="info-section" id="sensorSpecificInfo" style="display: none;">
                        <h3 class="info-section-title">
                            <i class="fas fa-cog"></i>
                            <span id="specificInfoTitle">설정 정보</span>
                        </h3>
                        <div class="info-grid" id="specificInfoContent">
                            <!-- 센서 타입별 동적 내용 -->
                        </div>
                    </div>
                </div>
                
                <!-- 우측: 데이터 패널 -->
                <div class="sensor-data-panel">
                    <!-- 상단: 실시간 차트 -->
                    <div class="realtime-chart-section">
                        <div class="section-header">
                            <h3 class="section-title">
                                <i class="fas fa-chart-line"></i>
                                실시간 데이터
                            </h3>
                            <div class="realtime-controls">
                                <button id="toggleRealtimeBtn" class="btn btn-sm btn-primary">
                                    <i class="fas fa-pause"></i>
                                    일시정지
                                </button>
                                <select id="realtimeInterval" class="form-select form-select-sm">
                                    <option value="60">최근 1분</option>
                                    <option value="300" selected>최근 5분</option>
                                    <option value="600">최근 10분</option>
                                    <option value="1800">최근 30분</option>
                                    <option value="3600">최근 1시간</option>
                                    <option value="limit">최근 50개</option>
                                </select>
                            </div>
                        </div>
                        <div class="chart-container">
                            <canvas id="realtimeChart"></canvas>
                        </div>
                        <div class="realtime-stats">
                            <div class="stat-item">
                                <span class="stat-label">현재값</span>
                                <span class="stat-value" id="currentValue">-</span>
                            </div>
                            <div class="stat-item">
                                <span class="stat-label">평균</span>
                                <span class="stat-value" id="avgValue">-</span>
                            </div>
                            <div class="stat-item">
                                <span class="stat-label">최대</span>
                                <span class="stat-value" id="maxValue">-</span>
                            </div>
                            <div class="stat-item">
                                <span class="stat-label">최소</span>
                                <span class="stat-value" id="minValue">-</span>
                            </div>
                        </div>
                    </div>
                    
                    <!-- 하단: 히스토리 데이터 조회 -->
                    <div class="history-data-section">
                        <div class="section-header">
                            <h3 class="section-title">
                                <i class="fas fa-database"></i>
                                히스토리 데이터 조회
                            </h3>
                        </div>
                        
                        <div class="history-controls">
                            <div class="date-range-picker">
                                <div class="date-input-group">
                                    <label>시작일시</label>
                                    <input type="text" id="historyStartDate" class="form-control" placeholder="시작일 선택">
                                </div>
                                <div class="date-input-group">
                                    <label>종료일시</label>
                                    <input type="text" id="historyEndDate" class="form-control" placeholder="종료일 선택">
                                </div>
                                <button id="loadHistoryBtn" class="btn btn-primary">
                                    <i class="fas fa-search"></i>
                                    조회
                                </button>
                                <button id="exportCsvBtn" class="btn btn-success" disabled>
                                    <i class="fas fa-file-csv"></i>
                                    CSV 내보내기
                                </button>
                                <button id="exportExcelBtn" class="btn btn-success" disabled>
                                    <i class="fas fa-file-excel"></i>
                                    Excel 내보내기
                                </button>
                            </div>
                        </div>
                        
                        <div class="history-grid-container">
                            <div id="historyDataGrid"></div>
                        </div>
                        
                        <div class="history-summary" id="historySummary" style="display: none;">
                            <span class="summary-item">
                                <i class="fas fa-chart-bar"></i>
                                총 <span id="totalRecords">0</span>개 레코드
                            </span>
                            <span class="summary-item">
                                <i class="fas fa-calendar-alt"></i>
                                기간: <span id="dataPeriod">-</span>
                            </span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    `;

    document.body.insertAdjacentHTML('beforeend', modalHTML);
    console.log('센서 상세 모달 HTML 직접 삽입 완료');
}

// 한국어 날짜 파싱 함수
function parseKoreanDateTime(dateTimeStr) {
    if (!dateTimeStr) return null;

    try {
        let date;

        // 한국어 날짜 형식 처리 (예: "2025-06-10 오후 9:35:01")
        if (typeof dateTimeStr === 'string' && (dateTimeStr.includes('오전') || dateTimeStr.includes('오후'))) {
            const isPM = dateTimeStr.includes('오후');
            const isAM = dateTimeStr.includes('오전');

            // "오전/오후" 제거
            let cleanStr = dateTimeStr.replace('오전', '').replace('오후', '').trim();

            // 날짜와 시간 분리
            const parts = cleanStr.split(' ');
            if (parts.length >= 2) {
                const datePart = parts[0];
                const timePart = parts[1];
                const timeParts = timePart.split(':');

                if (timeParts.length === 3) {
                    let hours = parseInt(timeParts[0]);

                    // 12시간제를 24시간제로 변환
                    if (isPM && hours !== 12) {
                        hours += 12;
                    } else if (isAM && hours === 12) {
                        hours = 0;
                    }

                    // ISO 형식으로 재구성
                    const isoString = `${datePart}T${hours.toString().padStart(2, '0')}:${timeParts[1]}:${timeParts[2]}`;
                    date = new Date(isoString);
                }
            }
        } else if (typeof dateTimeStr === 'string' && dateTimeStr.includes('T')) {
            // ISO 형식
            date = new Date(dateTimeStr);
        } else if (typeof dateTimeStr === 'string' && dateTimeStr.includes(' ')) {
            // "YYYY-MM-DD HH:mm:ss" 형식
            date = new Date(dateTimeStr.replace(' ', 'T'));
        } else {
            // 숫자(타임스탬프)인 경우
            date = new Date(dateTimeStr);
        }

        return date && !isNaN(date.getTime()) ? date : null;
    } catch (e) {
        console.error('날짜 파싱 오류:', e, dateTimeStr);
        return null;
    }
}

// 모달 초기화
function initializeSensorDetailModal() {
    // SyncFusion 라이센스 확인 및 재등록
    if (window.syncfusionLicenseKey && typeof ej !== 'undefined') {
        try {
            ej.base.registerLicense(window.syncfusionLicenseKey);
            console.log('센서 상세 모달 - SyncFusion 라이센스 등록');
        } catch (e) {
            console.warn('센서 상세 모달 - 라이센스 등록 실패:', e);
        }
    }

    // Date pickers 초기화
    if (typeof flatpickr !== 'undefined') {
        const dateConfig = {
            dateFormat: "Y-m-d H:i",
            enableTime: true,
            time_24hr: true,
            maxDate: "today",
            locale: "ko",
            theme: "dark"
        };

        historyStartPicker = flatpickr("#historyStartDate", {
            ...dateConfig,
            onChange: function (selectedDates) {
                if (historyEndPicker && selectedDates[0]) {
                    historyEndPicker.set('minDate', selectedDates[0]);
                }
            }
        });

        historyEndPicker = flatpickr("#historyEndDate", {
            ...dateConfig,
            onChange: function (selectedDates) {
                if (historyStartPicker && selectedDates[0]) {
                    historyStartPicker.set('maxDate', selectedDates[0]);
                }
            }
        });
    }

    // 이벤트 리스너 설정
    setupDetailModalEventListeners();
}

// 이벤트 리스너 설정
function setupDetailModalEventListeners() {
    // 실시간 차트 컨트롤
    document.getElementById('toggleRealtimeBtn')?.addEventListener('click', toggleRealtimeChart);
    document.getElementById('realtimeInterval')?.addEventListener('change', handleRealtimeIntervalChange);

    // 히스토리 데이터 컨트롤
    document.getElementById('loadHistoryBtn')?.addEventListener('click', loadHistoryData);
    document.getElementById('exportCsvBtn')?.addEventListener('click', exportDataAsCSV);
    document.getElementById('exportExcelBtn')?.addEventListener('click', exportDataAsExcel);
}

// 센서 상세 모달 열기
async function openSensorDetailModal(sensor) {
    if (!sensor) return;

    detailModalSensor = sensor;
    const modal = document.getElementById('sensorDetailModal');
    if (!modal) return;

    // 모달 표시
    modal.style.display = 'flex';

    // 센서 정보 표시
    updateSensorInfo(sensor);

    // 차트 초기화
    initializeRealtimeChart();

    // 실시간 데이터 시작
    startRealtimeDataUpdate();

    // 날짜 초기값 설정 (최근 7일)
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 7);

    if (historyStartPicker) historyStartPicker.setDate(startDate);
    if (historyEndPicker) historyEndPicker.setDate(endDate);
}

// 센서 정보 업데이트
function updateSensorInfo(sensor) {
    // 기본 정보
    document.getElementById('sensorDetailName').textContent = sensor.name || `센서 ${sensor.sensorID}`;
    document.getElementById('detailSensorId').textContent = sensor.sensorID;
    document.getElementById('detailSensorType').textContent = sensorTypeInfo[sensor.sensorType]?.name || sensor.sensorType;
    document.getElementById('detailSensorUUID').textContent = sensor.sensorUUID || '-';
    document.getElementById('detailSensorModel').textContent = sensor.model || '-';
    document.getElementById('detailFirmwareVersion').textContent = sensor.firmwareVersion || '-';
    document.getElementById('detailInstallDate').textContent = formatDateTime(sensor.installationDate);

    // 아이콘 설정
    document.getElementById('sensorDetailIcon').textContent = sensorTypeInfo[sensor.sensorType]?.icon || '📡';

    // 연결 정보
    const statusElement = document.getElementById('detailConnectionStatus');
    const statusIndicator = statusElement.querySelector('.status-indicator');
    const statusText = statusElement.querySelector('.status-text');

    statusText.textContent = sensor.connectionStatus === 'online' ? '온라인' : '오프라인';
    statusIndicator.className = `status-indicator ${sensor.connectionStatus !== 'online' ? 'offline' : ''}`;

    document.getElementById('detailLastComm').textContent = formatDateTime(sensor.lastCommunication);
    document.getElementById('detailHeartbeatInterval').textContent = `${sensor.heartbeatInterval || 60}초`;
    document.getElementById('detailTimeout').textContent = `${sensor.connectionTimeout || 180}초`;

    // 소속 정보
    const company = companies.find(c => c.companyID === selectedCompanyId);
    const group = sensorGroups.find(g => g.groupID === sensor.groupID);

    document.getElementById('detailCompany').textContent = company?.companyName || '-';
    document.getElementById('detailGroup').textContent = group?.groupName || '-';
    document.getElementById('detailLocation').textContent = group?.location || '-';

    // 센서 타입별 추가 정보
    updateSensorSpecificInfo(sensor);
}

// 센서 타입별 추가 정보 업데이트
function updateSensorSpecificInfo(sensor) {
    const specificSection = document.getElementById('sensorSpecificInfo');
    const titleElement = document.getElementById('specificInfoTitle');
    const contentElement = document.getElementById('specificInfoContent');

    contentElement.innerHTML = '';

    switch (sensor.sensorType) {
        case 'speaker':
            specificSection.style.display = 'block';
            titleElement.textContent = '스피커 설정';
            contentElement.innerHTML = `
                <div class="info-item">
                    <span class="info-label">전원 상태</span>
                    <span class="info-value">${sensor.latestData?.powerStatus ? '🔊 켜짐' : '🔇 꺼짐'}</span>
                </div>
                <div class="info-item">
                    <span class="info-label">볼륨</span>
                    <span class="info-value">${sensor.latestData?.volume || 0}%</span>
                </div>
                <div class="info-item">
                    <span class="info-label">주파수</span>
                    <span class="info-value">${sensor.latestData?.frequency || '-'} Hz</span>
                </div>
            `;
            break;

        case 'particle':
            specificSection.style.display = 'block';
            titleElement.textContent = '측정 범위';
            contentElement.innerHTML = `
                <div class="info-item">
                    <span class="info-label">측정 입자</span>
                    <span class="info-value">PM0.3 ~ PM10</span>
                </div>
                <div class="info-item">
                    <span class="info-label">측정 단위</span>
                    <span class="info-value">P-Counter</span>
                </div>
            `;
            break;

        case 'temp_humidity':
            specificSection.style.display = 'block';
            titleElement.textContent = '측정 범위';
            contentElement.innerHTML = `
                <div class="info-item">
                    <span class="info-label">온도 범위</span>
                    <span class="info-value">-40°C ~ 80°C</span>
                </div>
                <div class="info-item">
                    <span class="info-label">습도 범위</span>
                    <span class="info-value">0% ~ 100%</span>
                </div>
            `;
            break;

        case 'wind':
            specificSection.style.display = 'block';
            titleElement.textContent = '측정 범위';
            contentElement.innerHTML = `
                <div class="info-item">
                    <span class="info-label">풍속 범위</span>
                    <span class="info-value">0 ~ 60 m/s</span>
                </div>
                <div class="info-item">
                    <span class="info-label">정확도</span>
                    <span class="info-value">±0.3 m/s</span>
                </div>
            `;
            break;

        default:
            specificSection.style.display = 'none';
    }
}

// 실시간 차트 초기화
function initializeRealtimeChart() {
    const ctx = document.getElementById('realtimeChart')?.getContext('2d');
    if (!ctx) return;

    // 기존 차트 제거
    if (realtimeChart) {
        realtimeChart.destroy();
    }

    // 차트 데이터셋 구성
    const datasets = createDatasets(detailModalSensor.sensorType);

    // 차트 생성
    realtimeChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: [],
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: {
                duration: 0
            },
            plugins: {
                legend: {
                    display: datasets.length > 1,
                    labels: {
                        color: '#fff',
                        usePointStyle: true,
                        padding: 10,
                        font: {
                            size: 11
                        }
                    },
                    onClick: function (e, legendItem, legend) {
                        const index = legendItem.datasetIndex;
                        const ci = legend.chart;
                        const meta = ci.getDatasetMeta(index);

                        // 토글
                        meta.hidden = meta.hidden === null ? !ci.data.datasets[index].hidden : null;
                        ci.update();
                    }
                },
                title: {
                    display: false
                },
                tooltip: {
                    mode: 'index',
                    intersect: false,
                    callbacks: {
                        label: function (context) {
                            let label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            if (context.parsed.y !== null) {
                                label += context.parsed.y.toFixed(1);
                            }
                            return label;
                        }
                    }
                }
            },
            scales: {
                x: {
                    type: 'category', // 'time' 대신 'category' 사용
                    ticks: {
                        color: '#fff',
                        maxRotation: 45,
                        minRotation: 45,
                        autoSkip: true,
                        maxTicksLimit: 10
                    },
                    grid: {
                        color: 'rgba(255, 255, 255, 0.1)'
                    }
                },
                y: {
                    ticks: { color: '#fff' },
                    grid: { color: 'rgba(255, 255, 255, 0.1)' }
                },
                // 온습도 센서의 경우 두 번째 Y축 추가
                ...(detailModalSensor.sensorType === 'temp_humidity' ? {
                    y1: {
                        type: 'linear',
                        display: true,
                        position: 'right',
                        ticks: { color: '#fff' },
                        grid: { drawOnChartArea: false }
                    }
                } : {})
            }
        }
    });
}

// 센서 타입별 데이터셋 생성
function createDatasets(sensorType) {
    switch (sensorType) {
        case 'temp_humidity':
            return [
                {
                    label: '온도 (°C)',
                    data: [],
                    borderColor: 'rgb(255, 99, 132)',
                    backgroundColor: 'rgba(255, 99, 132, 0.1)',
                    tension: 0.4,
                    yAxisID: 'y'
                },
                {
                    label: '습도 (%)',
                    data: [],
                    borderColor: 'rgb(54, 162, 235)',
                    backgroundColor: 'rgba(54, 162, 235, 0.1)',
                    tension: 0.4,
                    yAxisID: 'y1'
                }
            ];

        case 'particle':
            return [
                {
                    label: 'PM0.3',
                    data: [],
                    borderColor: 'rgb(255, 99, 132)',
                    backgroundColor: 'rgba(255, 99, 132, 0.1)',
                    tension: 0.4,
                    hidden: true
                },
                {
                    label: 'PM0.5',
                    data: [],
                    borderColor: 'rgb(255, 159, 64)',
                    backgroundColor: 'rgba(255, 159, 64, 0.1)',
                    tension: 0.4,
                    hidden: false
                },
                {
                    label: 'PM1.0',
                    data: [],
                    borderColor: 'rgb(255, 205, 86)',
                    backgroundColor: 'rgba(255, 205, 86, 0.1)',
                    tension: 0.4,
                    hidden: false
                },
                {
                    label: 'PM2.5',
                    data: [],
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.1)',
                    tension: 0.4,
                    hidden: false
                },
                {
                    label: 'PM5.0',
                    data: [],
                    borderColor: 'rgb(54, 162, 235)',
                    backgroundColor: 'rgba(54, 162, 235, 0.1)',
                    tension: 0.4,
                    hidden: false
                },
                {
                    label: 'PM10',
                    data: [],
                    borderColor: 'rgb(153, 102, 255)',
                    backgroundColor: 'rgba(153, 102, 255, 0.1)',
                    tension: 0.4,
                    hidden: true
                }
            ];

        case 'wind':
            return [{
                label: '풍속 (m/s)',
                data: [],
                borderColor: 'rgb(153, 102, 255)',
                backgroundColor: 'rgba(153, 102, 255, 0.1)',
                tension: 0.4
            }];

        case 'speaker':
            return [{
                label: '볼륨',
                data: [],
                borderColor: 'rgb(255, 206, 86)',
                backgroundColor: 'rgba(255, 206, 86, 0.1)',
                tension: 0.4
            }];

        default:
            return [{
                label: '값',
                data: [],
                borderColor: 'rgb(75, 192, 192)',
                backgroundColor: 'rgba(75, 192, 192, 0.1)',
                tension: 0.4
            }];
    }
}

// 실시간 데이터 업데이트 시작
async function startRealtimeDataUpdate() {
    if (realtimeUpdateInterval) {
        clearInterval(realtimeUpdateInterval);
    }

    // 초기 데이터 로드
    await updateRealtimeData();

    // 주기적 업데이트 (5초마다)
    realtimeUpdateInterval = setInterval(async () => {
        if (!isRealtimePaused && detailModalSensor) {
            await updateRealtimeData();
        }
    }, 5000);
}

// 실시간 데이터 업데이트
async function updateRealtimeData() {
    if (!detailModalSensor || !realtimeChart) return;

    try {
        // 선택된 시간 범위
        const intervalSeconds = parseInt(document.getElementById('realtimeInterval').value);
        const endTime = new Date();
        const startTime = new Date(endTime.getTime() - intervalSeconds * 1000);

        console.log('실시간 데이터 요청:', {
            sensorID: detailModalSensor.sensorID,
            startTime: startTime.toISOString(),
            endTime: endTime.toISOString()
        });

        // 먼저 limit로 최신 데이터를 가져와서 확인
        const latestResponse = await apiCall(`/api/sensors/${detailModalSensor.sensorID}/data?limit=10`);

        if (latestResponse && latestResponse.ok) {
            const latestData = await latestResponse.json();
            console.log('최신 데이터 10개:', latestData);

            if (latestData && latestData.length > 0) {
                // 가장 최신 데이터의 시간 확인
                const newestTimestamp = new Date(latestData[0].timestamp || latestData[0].Timestamp);
                const oldestTimestamp = new Date(latestData[latestData.length - 1].timestamp || latestData[latestData.length - 1].Timestamp);

                console.log('데이터 시간 범위:', {
                    newest: newestTimestamp.toISOString(),
                    oldest: oldestTimestamp.toISOString(),
                    현재시간과차이: (endTime - newestTimestamp) / 1000 + '초'
                });
            }
        }

        // 원래 요청도 실행
        const response = await apiCall(
            `/api/sensors/${detailModalSensor.sensorID}/data?` +
            `startDate=${startTime.toISOString()}&endDate=${endTime.toISOString()}`
        );

        if (response && response.ok) {
            const data = await response.json();
            console.log('시간 범위 데이터 수신:', data.length + '개');

            if (data && data.length > 0) {
                updateRealtimeChart(data);
                updateRealtimeStats(data);
            } else {
                console.warn('받은 데이터가 비어있습니다. limit 방식으로 재시도...');

                // 데이터가 없으면 limit 방식으로 재시도
                const limitCount = Math.max(20, Math.floor(intervalSeconds / 60)); // 최소 20개
                const fallbackResponse = await apiCall(
                    `/api/sensors/${detailModalSensor.sensorID}/data?limit=${limitCount}`
                );

                if (fallbackResponse && fallbackResponse.ok) {
                    const fallbackData = await fallbackResponse.json();
                    console.log('Fallback 데이터 수신:', fallbackData.length + '개');

                    if (fallbackData && fallbackData.length > 0) {
                        // 시간 범위에 맞는 데이터만 필터링
                        const filteredData = fallbackData.filter(d => {
                            const timestamp = new Date(d.timestamp || d.Timestamp);
                            return timestamp >= startTime && timestamp <= endTime;
                        });

                        console.log('필터링된 데이터:', filteredData.length + '개');

                        if (filteredData.length > 0) {
                            updateRealtimeChart(filteredData);
                            updateRealtimeStats(filteredData);
                        } else {
                            // 필터링 후에도 데이터가 없으면 전체 데이터 사용
                            updateRealtimeChart(fallbackData);
                            updateRealtimeStats(fallbackData);
                        }
                    }
                }
            }
        } else {
            console.error('API 응답 오류:', response.status);
        }
    } catch (error) {
        console.error('실시간 데이터 업데이트 실패:', error);
    }
}

// 실시간 차트 업데이트
function updateRealtimeChart(data) {
    if (!realtimeChart || !data || data.length === 0) return;

    console.log('실시간 차트 업데이트 - 데이터 수:', data.length);
    if (data.length > 0) {
        console.log('첫 번째 데이터 샘플:', data[0]);
    }

    // 시간 레이블을 문자열로 변환
    const labels = data.map(d => {
        const date = parseKoreanDateTime(d.timestamp || d.Timestamp);
        return date ? date.toLocaleTimeString('ko-KR') : '';
    });

    // 센서 타입별 데이터 매핑
    switch (detailModalSensor.sensorType) {
        case 'temp_humidity':
            realtimeChart.data.labels = labels;
            realtimeChart.data.datasets[0].data = data.map(d => d.temperature);
            realtimeChart.data.datasets[1].data = data.map(d => d.humidity);
            break;

        case 'particle':
            realtimeChart.data.labels = labels;
            // 모든 PM 값 매핑
            realtimeChart.data.datasets[0].data = data.map(d =>
                d.pm0_3 || d.pM0_3 || d.PM0_3 || d['PM0.3'] || d['pm0.3']
            );
            realtimeChart.data.datasets[1].data = data.map(d =>
                d.pm0_5 || d.pM0_5 || d.PM0_5 || d['PM0.5'] || d['pm0.5']
            );
            realtimeChart.data.datasets[2].data = data.map(d =>
                d.pm1_0 || d.pM1_0 || d.PM1_0 || d['PM1.0'] || d['pm1.0'] || d.pm1 || d.PM1
            );
            realtimeChart.data.datasets[3].data = data.map(d =>
                d.pm2_5 || d.pM2_5 || d.PM2_5 || d['PM2.5'] || d['pm2.5']
            );
            realtimeChart.data.datasets[4].data = data.map(d =>
                d.pm5_0 || d.pM5_0 || d.PM5_0 || d['PM5.0'] || d['pm5.0'] || d.pm5 || d.PM5
            );
            realtimeChart.data.datasets[5].data = data.map(d =>
                d.pm10_0 || d.pM10_0 || d.PM10_0 || d.pm10 || d.pM10 || d.PM10 || d['PM10'] || d['pm10']
            );
            break;

        case 'wind':
            realtimeChart.data.labels = labels;
            realtimeChart.data.datasets[0].data = data.map(d => d.windSpeed);
            break;

        case 'speaker':
            realtimeChart.data.labels = labels;
            realtimeChart.data.datasets[0].data = data.map(d => d.volume);
            break;
    }

    console.log('차트 레이블:', realtimeChart.data.labels.slice(0, 5));
    console.log('차트 데이터셋 0:', realtimeChart.data.datasets[0].data.slice(0, 5));

    realtimeChart.update('none'); // 애니메이션 없이 업데이트
}

// 실시간 통계 업데이트
function updateRealtimeStats(data) {
    if (!data || data.length === 0) {
        document.getElementById('currentValue').textContent = '-';
        document.getElementById('avgValue').textContent = '-';
        document.getElementById('maxValue').textContent = '-';
        document.getElementById('minValue').textContent = '-';
        return;
    }

    let values = [];
    let currentValue = '-';

    // 센서 타입별 값 추출
    switch (detailModalSensor.sensorType) {
        case 'temp_humidity':
            values = data.map(d => d.temperature).filter(v => v !== null && v !== undefined);
            const lastTemp = data[data.length - 1];
            currentValue = (lastTemp.temperature !== null && lastTemp.temperature !== undefined) ?
                `${lastTemp.temperature.toFixed(1)}°C / ${lastTemp.humidity?.toFixed(1) || '-'}%` : '-';
            break;

        case 'particle':
            values = data.map(d =>
                d.pm2_5 || d.pM2_5 || d.PM2_5 || d['PM2.5'] || d['pm2.5']
            ).filter(v => v !== null && v !== undefined);
            const lastData = data[data.length - 1];
            const pm25Value = lastData.pm2_5 || lastData.pM2_5 || lastData.PM2_5 || lastData['PM2.5'] || lastData['pm2.5'];
            currentValue = pm25Value !== undefined && pm25Value !== null ?
                `PM2.5: ${pm25Value.toFixed(0)}` : '-';
            break;

        case 'wind':
            values = data.map(d => d.windSpeed).filter(v => v !== null && v !== undefined);
            const lastWind = data[data.length - 1];
            currentValue = (lastWind.windSpeed !== null && lastWind.windSpeed !== undefined) ?
                `${lastWind.windSpeed.toFixed(1)} m/s` : '-';
            break;

        case 'speaker':
            values = data.map(d => d.volume).filter(v => v !== null && v !== undefined);
            const lastSpeaker = data[data.length - 1];
            currentValue = lastSpeaker.volume !== undefined ?
                `${lastSpeaker.volume}%` : '-';
            break;
    }

    // 통계 계산
    if (values.length > 0) {
        const avg = values.reduce((a, b) => a + b, 0) / values.length;
        const max = Math.max(...values);
        const min = Math.min(...values);

        document.getElementById('currentValue').textContent = currentValue;
        document.getElementById('avgValue').textContent = avg.toFixed(1);
        document.getElementById('maxValue').textContent = max.toFixed(1);
        document.getElementById('minValue').textContent = min.toFixed(1);
    } else {
        console.warn('유효한 데이터 값이 없습니다.');
    }
}

// 실시간 차트 토글
function toggleRealtimeChart() {
    isRealtimePaused = !isRealtimePaused;
    const btn = document.getElementById('toggleRealtimeBtn');

    if (isRealtimePaused) {
        btn.innerHTML = '<i class="fas fa-play"></i> 재개';
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-success');
    } else {
        btn.innerHTML = '<i class="fas fa-pause"></i> 일시정지';
        btn.classList.remove('btn-success');
        btn.classList.add('btn-primary');
    }
}

// 실시간 간격 변경
function handleRealtimeIntervalChange() {
    const intervalValue = document.getElementById('realtimeInterval').value;

    // 차트 초기화 (새로운 데이터 범위에 맞게)
    if (realtimeChart) {
        realtimeChart.data.labels = [];
        realtimeChart.data.datasets.forEach(dataset => {
            dataset.data = [];
        });
        realtimeChart.update();
    }

    // 데이터 재로드
    updateRealtimeData();
}

// 히스토리 데이터 로드
async function loadHistoryData() {
    if (!detailModalSensor || !historyStartPicker || !historyEndPicker) return;

    const startDate = historyStartPicker.selectedDates[0];
    const endDate = historyEndPicker.selectedDates[0];

    if (!startDate || !endDate) {
        showToast({
            message: '조회 기간을 선택해주세요.',
            type: 'warning'
        });
        return;
    }

    showLoading(true);

    try {
        const response = await apiCall(
            `/api/sensors/${detailModalSensor.sensorID}/data?` +
            `startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`
        );

        if (response && response.ok) {
            const data = await response.json();
            displayHistoryGrid(data);
            updateHistorySummary(data, startDate, endDate);

            // 내보내기 버튼 활성화
            document.getElementById('exportCsvBtn').disabled = false;
            document.getElementById('exportExcelBtn').disabled = false;
        }
    } catch (error) {
        console.error('히스토리 데이터 로드 실패:', error);
        showToast({
            message: '데이터를 불러오는데 실패했습니다.',
            type: 'error'
        });
    } finally {
        showLoading(false);
    }
}

// 히스토리 그리드 표시
function displayHistoryGrid(data) {
    const gridDiv = document.getElementById('historyDataGrid');

    console.log('히스토리 데이터 로드:', data.length + '개');

    // 첫 번째 데이터의 타임스탬프 형식 확인 (디버깅용)
    if (data.length > 0) {
        console.log('타임스탬프 형식 샘플:', data[0].Timestamp, typeof data[0].Timestamp);
    }

    // 기존 그리드 제거
    if (historyDataGrid) {
        historyDataGrid.destroy();
        historyDataGrid = null;
    }
    gridDiv.innerHTML = '';

    // 테이블 HTML 생성
    let tableHtml = '<table id="historyTable" class="table table-dark table-striped" style="width:100%"><thead><tr>';

    // 헤더 생성
    tableHtml += '<th>시간</th>';

    switch (detailModalSensor.sensorType) {
        case 'temp_humidity':
            tableHtml += '<th>온도 (°C)</th><th>습도 (%)</th>';
            break;
        case 'particle':
            tableHtml += '<th>PM0.3</th><th>PM0.5</th><th>PM1.0</th><th>PM2.5</th><th>PM5.0</th><th>PM10</th>';
            break;
        case 'wind':
            tableHtml += '<th>풍속 (m/s)</th>';
            break;
        case 'speaker':
            tableHtml += '<th>전원</th><th>볼륨</th><th>주파수 (Hz)</th>';
            break;
    }

    tableHtml += '</tr></thead><tbody>';

    // 데이터 행 생성
    data.forEach(row => {
        tableHtml += '<tr>';

        // 타임스탬프 처리
        let dateStr = '-';
        const date = parseKoreanDateTime(row.Timestamp);
        if (date) {
            dateStr = date.toLocaleString('ko-KR');
        } else if (row.Timestamp) {
            // 파싱 실패 시 원본 사용
            dateStr = row.Timestamp;
        }
        tableHtml += `<td>${dateStr}</td>`;

        switch (detailModalSensor.sensorType) {
            case 'temp_humidity':
                tableHtml += `<td>${row.temperature?.toFixed(2) || '-'}</td>`;
                tableHtml += `<td>${row.humidity?.toFixed(2) || '-'}</td>`;
                break;
            case 'particle':
                // 대소문자 구분 없이 값 가져오기
                const getValue = (fieldNames) => {
                    for (const field of fieldNames) {
                        if (row[field] !== undefined && row[field] !== null) {
                            return row[field];
                        }
                    }
                    return '-';
                };

                tableHtml += `<td>${getValue(['pm0_3', 'PM0_3', 'pM0_3'])}</td>`;
                tableHtml += `<td>${getValue(['pm0_5', 'PM0_5', 'pM0_5'])}</td>`;
                tableHtml += `<td>${getValue(['pm1_0', 'PM1_0', 'pM1_0'])}</td>`;
                tableHtml += `<td>${getValue(['pm2_5', 'PM2_5', 'pM2_5'])}</td>`;
                tableHtml += `<td>${getValue(['pm5_0', 'PM5_0', 'pM5_0'])}</td>`;
                tableHtml += `<td>${getValue(['pm10_0', 'PM10_0', 'pM10_0', 'pm10', 'PM10'])}</td>`;
                break;
            case 'wind':
                tableHtml += `<td>${row.windSpeed?.toFixed(2) || '-'}</td>`;
                break;
            case 'speaker':
                tableHtml += `<td>${row.powerStatus ? 'ON' : 'OFF'}</td>`;
                tableHtml += `<td>${row.volume || '-'}</td>`;
                tableHtml += `<td>${row.frequency?.toFixed(1) || '-'}</td>`;
                break;
        }

        tableHtml += '</tr>';
    });

    tableHtml += '</tbody></table>';
    gridDiv.innerHTML = tableHtml;

    // DataTables 초기화
    try {
        // jQuery와 DataTables 존재 확인
        if (typeof $ === 'undefined' || typeof $.fn.DataTable === 'undefined') {
            console.error('jQuery 또는 DataTables가 로드되지 않았습니다.');
            throw new Error('DataTables not available');
        }

        historyDataGrid = $('#historyTable').DataTable({
            pageLength: 50,
            lengthMenu: [[20, 50, 100, 200, -1], [20, 50, 100, 200, "전체"]],
            order: [[0, 'desc']],
            scrollY: '400px',
            scrollCollapse: true,
            scrollX: true,
            paging: true,
            info: true,
            searching: true,
            dom: '<"top"lf>rt<"bottom"ip><"clear">',  // 커스텀 DOM 구조
            language: {
                "decimal": "",
                "emptyTable": "데이터가 없습니다",
                "info": "_START_ - _END_ / 전체 _TOTAL_개",
                "infoEmpty": "0개",
                "infoFiltered": "(전체 _MAX_개 중 검색결과)",
                "infoPostFix": "",
                "thousands": ",",
                "lengthMenu": "_MENU_개씩 보기",
                "loadingRecords": "로딩중...",
                "processing": "처리중...",
                "search": "검색:",
                "zeroRecords": "검색된 데이터가 없습니다",
                "paginate": {
                    "first": "첫 페이지",
                    "last": "마지막 페이지",
                    "next": "다음",
                    "previous": "이전"
                },
                "aria": {
                    "sortAscending": ": 오름차순 정렬",
                    "sortDescending": ": 내림차순 정렬"
                }
            }
        });

        // 내보내기 버튼은 그대로 유지
        document.getElementById('exportCsvBtn').style.display = 'inline-block';
        document.getElementById('exportExcelBtn').style.display = 'inline-block';
        document.getElementById('exportCsvBtn').disabled = false;
        document.getElementById('exportExcelBtn').disabled = false;

        console.log('DataTables 초기화 완료');

    } catch (error) {
        console.error('DataTables 초기화 오류:', error);
        // DataTables 초기화 실패 시에도 버튼 표시
        document.getElementById('exportCsvBtn').style.display = 'inline-block';
        document.getElementById('exportExcelBtn').style.display = 'inline-block';
        document.getElementById('exportCsvBtn').disabled = false;
        document.getElementById('exportExcelBtn').disabled = false;
    }
}

// 히스토리 요약 정보 업데이트
function updateHistorySummary(data, startDate, endDate) {
    const summaryDiv = document.getElementById('historySummary');
    summaryDiv.style.display = 'block';

    document.getElementById('totalRecords').textContent = data.length.toLocaleString();

    const period = `${startDate.toLocaleDateString('ko-KR')} ~ ${endDate.toLocaleDateString('ko-KR')}`;
    document.getElementById('dataPeriod').textContent = period;
}

// CSV 내보내기
function exportDataAsCSV() {
    if (!historyDataGrid) return;

    const fileName = `sensor_${detailModalSensor.sensorID}_${new Date().toISOString().slice(0, 10)}.csv`;

    // DataTables에서 모든 데이터 가져오기
    const data = historyDataGrid.data().toArray();
    const headers = historyDataGrid.columns().header().toArray().map(th => $(th).text());

    // CSV 데이터 생성
    let csvContent = '\uFEFF'; // BOM for UTF-8

    // 헤더 추가
    csvContent += headers.join(',') + '\n';

    // 데이터 추가
    data.forEach(row => {
        const rowData = [];
        historyDataGrid.columns().every(function (index) {
            const cell = historyDataGrid.cell(row, index).data();
            const value = String(cell || '');
            if (value.includes(',') || value.includes('\n') || value.includes('"')) {
                rowData.push(`"${value.replace(/"/g, '""')}"`);
            } else {
                rowData.push(value);
            }
            return true;
        });
        csvContent += rowData.join(',') + '\n';
    });

    // 다운로드
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(link.href);

    showToast({
        message: 'CSV 파일이 다운로드됩니다.',
        type: 'success'
    });
}

// Excel 내보내기
function exportDataAsExcel() {
    if (!historyDataGrid) return;

    // XLSX 라이브러리가 로드되지 않은 경우 CSV로 대체
    if (typeof XLSX === 'undefined') {
        console.warn('XLSX 라이브러리가 로드되지 않았습니다. CSV로 내보냅니다.');
        exportDataAsCSV();
        return;
    }

    const fileName = `sensor_${detailModalSensor.sensorID}_${new Date().toISOString().slice(0, 10)}.xlsx`;

    // DataTables에서 모든 데이터 가져오기
    const data = historyDataGrid.data().toArray();
    const headers = historyDataGrid.columns().header().toArray().map(th => $(th).text());

    // 워크북 생성
    const wb = XLSX.utils.book_new();

    // 데이터 준비 (헤더 + 데이터)
    const wsData = [headers];

    data.forEach(row => {
        const rowData = [];
        historyDataGrid.columns().every(function (index) {
            rowData.push(historyDataGrid.cell(row, index).data());
            return true;
        });
        wsData.push(rowData);
    });

    // 워크시트 생성
    const ws = XLSX.utils.aoa_to_sheet(wsData);

    // 컬럼 너비 자동 조정
    const colWidths = headers.map((header, i) => {
        const maxLength = Math.max(
            header.length,
            ...data.map(row => String(historyDataGrid.cell(row, i).data() || '').length)
        );
        return { wch: Math.min(maxLength + 2, 30) };
    });
    ws['!cols'] = colWidths;

    // 워크시트를 워크북에 추가
    XLSX.utils.book_append_sheet(wb, ws, '센서데이터');

    // 파일 다운로드
    XLSX.writeFile(wb, fileName);

    showToast({
        message: 'Excel 파일이 다운로드됩니다.',
        type: 'success'
    });
}

// 모달 닫기
function closeSensorDetailModal() {
    const modal = document.getElementById('sensorDetailModal');
    if (modal) {
        modal.style.display = 'none';
    }

    // 실시간 업데이트 중지
    if (realtimeUpdateInterval) {
        clearInterval(realtimeUpdateInterval);
        realtimeUpdateInterval = null;
    }

    // 차트 제거
    if (realtimeChart) {
        realtimeChart.destroy();
        realtimeChart = null;
    }

    // 그리드 제거
    if (historyDataGrid) {
        historyDataGrid = null;
    }

    // 상태 초기화
    detailModalSensor = null;
    isRealtimePaused = false;
}

// 모달 외부 클릭 시 닫기
document.getElementById('sensorDetailModal')?.addEventListener('click', function (e) {
    if (e.target === this) {
        closeSensorDetailModal();
    }
});

// ESC 키로 모달 닫기
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && document.getElementById('sensorDetailModal')?.style.display === 'flex') {
        closeSensorDetailModal();
    }
});