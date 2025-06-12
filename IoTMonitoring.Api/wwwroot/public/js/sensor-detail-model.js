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
    try {
            insertSensorDetailModalHTML();
    } catch (error) {
        console.error('센서 상세 모달 로드 중 오류:', error);
        // 대체 HTML 직접 삽입
        insertSensorDetailModalHTML();
    }
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
                            </div>
                            
                            <div class="export-controls">
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
    console.log('날짜 형식:', dateTimeStr);
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
                    labels: { color: '#fff' }
                },
                title: {
                    display: false
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
                    label: 'PM2.5',
                    data: [],
                    borderColor: 'rgb(255, 99, 132)',
                    backgroundColor: 'rgba(255, 99, 132, 0.1)',
                    tension: 0.4
                },
                {
                    label: 'PM10',
                    data: [],
                    borderColor: 'rgb(54, 162, 235)',
                    backgroundColor: 'rgba(54, 162, 235, 0.1)',
                    tension: 0.4
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
        const intervalMinutes = parseInt(document.getElementById('realtimeInterval').value) / 60;
        const endTime = new Date();
        const startTime = new Date(endTime.getTime() - intervalMinutes * 60 * 1000);

        // 데이터 가져오기
        const response = await apiCall(
            `/api/sensors/${detailModalSensor.sensorID}/data?` +
            `startDate=${startTime.toISOString()}&endDate=${endTime.toISOString()}`
        );

        if (response && response.ok) {
            const data = await response.json();
            updateRealtimeChart(data);
            updateRealtimeStats(data);
        }
    } catch (error) {
        console.error('실시간 데이터 업데이트 실패:', error);
    }
}

// 실시간 차트 업데이트
function updateRealtimeChart(data) {
    if (!realtimeChart || !data || data.length === 0) return;

    // 시간 레이블을 문자열로 변환
    const labels = data.map(d => {
        const date = parseKoreanDateTime(d.timestamp);
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
            realtimeChart.data.datasets[0].data = data.map(d => d.pm2_5 || d.pM2_5 || d.PM2_5);
            if (realtimeChart.data.datasets[1]) {
                realtimeChart.data.datasets[1].data = data.map(d => d.pm10 || d.pM10 || d.PM10);
            }
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
            currentValue = data[data.length - 1].temperature ?
                `${data[data.length - 1].temperature.toFixed(1)}°C / ${data[data.length - 1].humidity.toFixed(1)}%` : '-';
            break;

        case 'particle':
            values = data.map(d => d.pm2_5 || d.pM2_5 || d.PM2_5).filter(v => v !== null && v !== undefined);
            const lastData = data[data.length - 1];
            currentValue = (lastData.pm2_5 || lastData.pM2_5 || lastData.PM2_5) ?
                `PM2.5: ${(lastData.pm2_5 || lastData.pM2_5 || lastData.PM2_5).toFixed(0)}` : '-';
            break;

        case 'wind':
            values = data.map(d => d.windSpeed).filter(v => v !== null && v !== undefined);
            currentValue = data[data.length - 1].windSpeed ?
                `${data[data.length - 1].windSpeed.toFixed(1)} m/s` : '-';
            break;

        case 'speaker':
            values = data.map(d => d.volume).filter(v => v !== null && v !== undefined);
            currentValue = data[data.length - 1].volume !== undefined ?
                `${data[data.length - 1].volume}%` : '-';
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
    // 차트 데이터 리셋 후 재로드
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
        console.log('타임스탬프 형식 샘플:', data[0].timestamp, typeof data[0].timestamp);
    }

    // 기존 내용 제거
    gridDiv.innerHTML = '';

    // 컨테이너 생성
    const container = document.createElement('div');
    container.className = 'history-table-container';

    // 검색 및 페이지 설정 컨트롤
    const controls = document.createElement('div');
    controls.className = 'table-controls mb-3';
    controls.innerHTML = `
        <div class="d-flex justify-content-between align-items-center">
            <div class="d-flex align-items-center gap-2">
                <input type="text" id="historySearch" class="form-control form-control-sm" placeholder="검색..." style="width: 200px;">
                <select id="historyPageSize" class="form-select form-select-sm" style="width: auto;">
                    <option value="20">20개</option>
                    <option value="50" selected>50개</option>
                    <option value="100">100개</option>
                    <option value="200">200개</option>
                    <option value="-1">전체</option>
                </select>
            </div>
            <div id="historyTableInfo" class="text-muted"></div>
        </div>
    `;
    container.appendChild(controls);

    // 테이블 생성
    const table = document.createElement('table');
    table.className = 'table table-dark table-striped table-hover';
    table.id = 'historyTable';

    // 헤더 생성
    const thead = document.createElement('thead');
    const headerRow = document.createElement('tr');

    // 헤더 컬럼 정의
    const headers = ['시간'];
    switch (detailModalSensor.sensorType) {
        case 'temp_humidity':
            headers.push('온도 (°C)', '습도 (%)');
            break;
        case 'particle':
            headers.push('PM0.3', 'PM0.5', 'PM1.0', 'PM2.5', 'PM5.0', 'PM10');
            break;
        case 'wind':
            headers.push('풍속 (m/s)');
            break;
        case 'speaker':
            headers.push('전원', '볼륨', '주파수 (Hz)');
            break;
    }

    headers.forEach((header, index) => {
        const th = document.createElement('th');
        th.innerHTML = `${header} <span class="sort-icon" data-col="${index}">⇅</span>`;
        th.style.cursor = 'pointer';
        headerRow.appendChild(th);
    });

    thead.appendChild(headerRow);
    table.appendChild(thead);

    // 바디 생성
    const tbody = document.createElement('tbody');

    // 데이터를 테이블용 배열로 변환
    const tableData = data.map(row => {
        const rowData = [];

        // 타임스탬프 처리
        let dateStr = '-';
        const date = parseKoreanDateTime(row.Timestamp);

        console.log('타임스탬프 파싱:', row.Timestamp, date, typeof date);

        if (date) {
            dateStr = date.toLocaleString('ko-KR');
        } else if (row.Timestamp) {
            // 파싱 실패 시 원본 사용
            dateStr = row.Timestamp;
        }
        rowData.push(dateStr);

        // 센서 타입별 데이터 추가
        switch (detailModalSensor.sensorType) {
            case 'temp_humidity':
                rowData.push(
                    row.temperature?.toFixed(2) || '-',
                    row.humidity?.toFixed(2) || '-'
                );
                break;
            case 'particle':
                const getValue = (fieldNames) => {
                    for (const field of fieldNames) {
                        if (row[field] !== undefined && row[field] !== null) {
                            return row[field];
                        }
                    }
                    return '-';
                };
                rowData.push(
                    getValue(['pm0_3', 'PM0_3', 'pM0_3']),
                    getValue(['pm0_5', 'PM0_5', 'pM0_5']),
                    getValue(['pm1_0', 'PM1_0', 'pM1_0']),
                    getValue(['pm2_5', 'PM2_5', 'pM2_5']),
                    getValue(['pm5_0', 'PM5_0', 'pM5_0']),
                    getValue(['pm10_0', 'PM10_0', 'pM10_0', 'pm10', 'PM10'])
                );
                break;
            case 'wind':
                rowData.push(row.windSpeed?.toFixed(2) || '-');
                break;
            case 'speaker':
                rowData.push(
                    row.powerStatus ? 'ON' : 'OFF',
                    row.volume || '-',
                    row.frequency?.toFixed(1) || '-'
                );
                break;
        }

        return rowData;
    });

    table.appendChild(tbody);
    container.appendChild(table);

    // 페이지네이션 컨테이너
    const pagination = document.createElement('div');
    pagination.className = 'pagination-container d-flex justify-content-center mt-3';
    container.appendChild(pagination);

    gridDiv.appendChild(container);

    // 테이블 기능 구현
    let currentPage = 1;
    let pageSize = 50;
    let sortColumn = 0;
    let sortDirection = 'desc';
    let filteredData = [...tableData];

    // 테이블 업데이트 함수
    function updateTable() {
        const searchTerm = document.getElementById('historySearch').value.toLowerCase();

        // 필터링
        filteredData = tableData.filter(row =>
            row.some(cell => String(cell).toLowerCase().includes(searchTerm))
        );

        // 정렬
        filteredData.sort((a, b) => {
            let aVal = a[sortColumn];
            let bVal = b[sortColumn];

            // 숫자 변환 시도
            if (!isNaN(parseFloat(aVal)) && !isNaN(parseFloat(bVal))) {
                aVal = parseFloat(aVal);
                bVal = parseFloat(bVal);
            }

            if (sortDirection === 'asc') {
                return aVal > bVal ? 1 : -1;
            } else {
                return aVal < bVal ? 1 : -1;
            }
        });

        // 페이지 크기
        const actualPageSize = pageSize === -1 ? filteredData.length : pageSize;
        const totalPages = Math.ceil(filteredData.length / actualPageSize);

        // 현재 페이지 데이터
        const startIndex = (currentPage - 1) * actualPageSize;
        const endIndex = startIndex + actualPageSize;
        const pageData = filteredData.slice(startIndex, endIndex);

        // 테이블 바디 업데이트
        tbody.innerHTML = '';
        pageData.forEach(rowData => {
            const tr = document.createElement('tr');
            rowData.forEach(cell => {
                const td = document.createElement('td');
                td.textContent = cell;
                tr.appendChild(td);
            });
            tbody.appendChild(tr);
        });

        // 정보 업데이트
        const info = document.getElementById('historyTableInfo');
        info.textContent = `${startIndex + 1} - ${Math.min(endIndex, filteredData.length)} / 전체 ${filteredData.length}개`;

        // 페이지네이션 업데이트
        updatePagination(totalPages);
    }

    // 페이지네이션 업데이트
    function updatePagination(totalPages) {
        pagination.innerHTML = '';

        if (totalPages <= 1) return;

        const nav = document.createElement('nav');
        const ul = document.createElement('ul');
        ul.className = 'pagination';

        // 이전 버튼
        const prevLi = document.createElement('li');
        prevLi.className = `page-item ${currentPage === 1 ? 'disabled' : ''}`;
        prevLi.innerHTML = '<a class="page-link" href="#">이전</a>';
        prevLi.onclick = () => {
            if (currentPage > 1) {
                currentPage--;
                updateTable();
            }
        };
        ul.appendChild(prevLi);

        // 페이지 번호
        for (let i = 1; i <= Math.min(totalPages, 10); i++) {
            const li = document.createElement('li');
            li.className = `page-item ${i === currentPage ? 'active' : ''}`;
            li.innerHTML = `<a class="page-link" href="#">${i}</a>`;
            li.onclick = () => {
                currentPage = i;
                updateTable();
            };
            ul.appendChild(li);
        }

        // 다음 버튼
        const nextLi = document.createElement('li');
        nextLi.className = `page-item ${currentPage === totalPages ? 'disabled' : ''}`;
        nextLi.innerHTML = '<a class="page-link" href="#">다음</a>';
        nextLi.onclick = () => {
            if (currentPage < totalPages) {
                currentPage++;
                updateTable();
            }
        };
        ul.appendChild(nextLi);

        nav.appendChild(ul);
        pagination.appendChild(nav);
    }

    // 이벤트 리스너
    document.getElementById('historySearch').addEventListener('input', () => {
        currentPage = 1;
        updateTable();
    });

    document.getElementById('historyPageSize').addEventListener('change', (e) => {
        pageSize = parseInt(e.target.value);
        currentPage = 1;
        updateTable();
    });

    // 정렬 이벤트
    headerRow.querySelectorAll('th').forEach((th, index) => {
        th.addEventListener('click', () => {
            if (sortColumn === index) {
                sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
            } else {
                sortColumn = index;
                sortDirection = 'desc';
            }
            updateTable();
        });
    });

    // 초기 테이블 렌더링
    updateTable();

    // 내보내기 버튼 활성화
    document.getElementById('exportCsvBtn').style.display = 'inline-block';
    document.getElementById('exportExcelBtn').style.display = 'inline-block';

    // 그리드 데이터 저장 (내보내기용)
    historyDataGrid = {
        data: tableData,
        headers: headers,
        rawData: data
    };
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
    if (!historyDataGrid || !historyDataGrid.data) return;

    const fileName = `sensor_${detailModalSensor.sensorID}_${new Date().toISOString().slice(0, 10)}.csv`;

    // CSV 데이터 생성
    let csvContent = '\uFEFF'; // BOM for UTF-8

    // 헤더 추가
    csvContent += historyDataGrid.headers.join(',') + '\n';

    // 데이터 추가
    historyDataGrid.data.forEach(row => {
        csvContent += row.map(cell => {
            // 값에 쉼표나 줄바꿈이 있으면 따옴표로 감싸기
            const value = String(cell);
            if (value.includes(',') || value.includes('\n') || value.includes('"')) {
                return `"${value.replace(/"/g, '""')}"`;
            }
            return value;
        }).join(',') + '\n';
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
    if (!historyDataGrid || !historyDataGrid.data) return;

    // XLSX 라이브러리가 로드되지 않은 경우 CSV로 대체
    if (typeof XLSX === 'undefined') {
        console.warn('XLSX 라이브러리가 로드되지 않았습니다. CSV로 내보냅니다.');
        exportDataAsCSV();
        return;
    }

    const fileName = `sensor_${detailModalSensor.sensorID}_${new Date().toISOString().slice(0, 10)}.xlsx`;

    // 워크북 생성
    const wb = XLSX.utils.book_new();

    // 데이터 준비 (헤더 + 데이터)
    const wsData = [historyDataGrid.headers, ...historyDataGrid.data];

    // 워크시트 생성
    const ws = XLSX.utils.aoa_to_sheet(wsData);

    // 컬럼 너비 자동 조정
    const colWidths = historyDataGrid.headers.map((header, i) => {
        const maxLength = Math.max(
            header.length,
            ...historyDataGrid.data.map(row => String(row[i]).length)
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