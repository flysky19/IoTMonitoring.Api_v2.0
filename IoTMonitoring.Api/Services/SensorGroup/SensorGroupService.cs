using IoTMonitoring.Api.DTOs;
using IoTMonitoring.Api.Services.SensorGroup.Interfaces;
using IoTMonitoring.Api.Data.Repositories.Interfaces;
using IoTMonitoring.Api.Mappers.Interfaces;
using Microsoft.Extensions.Logging;
using IoTMonitoring.Api.Utilities;

namespace IoTMonitoring.Api.Services.SensorGroup
{
    public class SensorGroupService : ISensorGroupService
    {
        private readonly ISensorGroupRepository _sensorGroupRepository;
        private readonly ISensorRepository _sensorRepository;
        private readonly ISensorGroupMapper _sensorGroupMapper;
        private readonly ISensorMapper _sensorMapper;
        private readonly ILogger<SensorGroupService> _logger;

        public SensorGroupService(
            ISensorGroupRepository sensorGroupRepository,
            ISensorRepository sensorRepository,
            ISensorGroupMapper sensorGroupMapper,
            ISensorMapper sensorMapper,
            ILogger<SensorGroupService> logger)
        {
            _sensorGroupRepository = sensorGroupRepository;
            _sensorRepository = sensorRepository;
            _sensorGroupMapper = sensorGroupMapper;
            _sensorMapper = sensorMapper;
            _logger = logger;
        }

        public async Task<IEnumerable<SensorGroupDto>> GetAllSensorGroupsAsync(int? companyId = null)
        {
            try
            {
                _logger.LogInformation($"센서 그룹 목록 조회 시작 - CompanyId: {companyId}");

                var groups = await _sensorGroupRepository.GetAllWithCompanyAsync(companyId);
                var groupDtos = new List<SensorGroupDto>();

                foreach (var group in groups)
                {
                    var groupDto = _sensorGroupMapper.ToDto(group);

                    // 새로 만든 Repository 메서드 사용
                    groupDto.SensorCount = await _sensorRepository.GetCountByGroupIdAsync(group.GroupID);

                    groupDtos.Add(groupDto);
                }

                _logger.LogInformation($"센서 그룹 목록 조회 완료 - 조회된 그룹 수: {groupDtos.Count}");
                return groupDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 목록 조회 실패: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<SensorGroupDetailDto> GetSensorGroupDetailAsync(int groupId)
        {
            try
            {
                _logger.LogInformation($"센서 그룹 상세 조회 시작 - GroupId: {groupId}");

                var group = await _sensorGroupRepository.GetByIdWithCompanyAsync(groupId);
                if (group == null)
                {
                    throw new KeyNotFoundException($"센서 그룹을 찾을 수 없습니다. ID: {groupId}");
                }

                var groupDetailDto = _sensorGroupMapper.ToDetailDto(group);

                // Repository 메서드들 활용
                groupDetailDto.SensorCount = await _sensorRepository.GetCountByGroupIdAsync(groupId);
                groupDetailDto.SensorCountByType = await _sensorRepository.GetCountBySensorTypeAsync(groupId);
                groupDetailDto.SensorCountByStatus = await _sensorRepository.GetCountByConnectionStatusAsync(groupId);

                _logger.LogInformation($"센서 그룹 상세 조회 완료 - GroupId: {groupId}");
                return groupDetailDto;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 상세 조회 실패 - GroupId: {groupId}, Error: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<SensorGroupDetailDto> CreateSensorGroupAsync(SensorGroupCreateDto createDto)
        {
            try
            {
                _logger.LogInformation($"센서 그룹 생성 시작 - Name: {createDto.GroupName}");

                // 중복 이름 체크 (같은 회사 내에서)
                var existingGroup = await _sensorGroupRepository.GetByNameAndCompanyAsync(createDto.GroupName, createDto.CompanyID);
                if (existingGroup != null)
                {
                    throw new InvalidOperationException($"같은 이름의 센서 그룹이 이미 존재합니다: {createDto.GroupName}");
                }

                var group = _sensorGroupMapper.ToEntity(createDto);
                group.CreatedAt = DateTimeHelper.Now;

                var createdGroup = await _sensorGroupRepository.CreateAsync(group);
                var result = await GetSensorGroupDetailAsync(createdGroup.GroupID);

                _logger.LogInformation($"센서 그룹 생성 완료 - GroupId: {result.GroupID}, Name: {result.GroupName}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 생성 실패 - Name: {createDto.GroupName}, Error: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<SensorGroupDetailDto> UpdateSensorGroupAsync(int groupId, SensorGroupUpdateDto updateDto)
        {
            try
            {
                _logger.LogInformation($"센서 그룹 수정 시작 - GroupId: {groupId}");

                var existingGroup = await _sensorGroupRepository.GetByIdAsync(groupId);
                if (existingGroup == null)
                {
                    throw new KeyNotFoundException($"센서 그룹을 찾을 수 없습니다. ID: {groupId}");
                }

                // 이름 중복 체크 (자기 자신 제외)
                if (!string.IsNullOrEmpty(updateDto.GroupName) && updateDto.GroupName != existingGroup.GroupName)
                {
                    var duplicateGroup = await _sensorGroupRepository.GetByNameAndCompanyAsync(updateDto.GroupName, existingGroup.CompanyID);
                    if (duplicateGroup != null && duplicateGroup.GroupID != groupId)
                    {
                        throw new InvalidOperationException($"같은 이름의 센서 그룹이 이미 존재합니다: {updateDto.GroupName}");
                    }
                }

                // 엔티티 업데이트
                _sensorGroupMapper.UpdateEntity(existingGroup, updateDto);
                existingGroup.UpdatedAt = DateTimeHelper.Now;

                await _sensorGroupRepository.UpdateAsync(existingGroup);
                var result = await GetSensorGroupDetailAsync(groupId);

                _logger.LogInformation($"센서 그룹 수정 완료 - GroupId: {groupId}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 수정 실패 - GroupId: {groupId}, Error: {ex.Message}", ex);
                throw;
            }
        }

        public async Task DeleteSensorGroupAsync(int groupId)
        {
            try
            {
                _logger.LogInformation($"센서 그룹 삭제 시작 - GroupId: {groupId}");

                var group = await _sensorGroupRepository.GetByIdAsync(groupId);
                if (group == null)
                {
                    throw new KeyNotFoundException($"센서 그룹을 찾을 수 없습니다. ID: {groupId}");
                }

                // Repository 메서드로 센서 개수 확인
                var sensorCount = await _sensorRepository.GetCountByGroupIdAsync(groupId);
                if (sensorCount > 0)
                {
                    throw new InvalidOperationException($"그룹에 센서가 {sensorCount}개 있어서 삭제할 수 없습니다. 먼저 센서들을 다른 그룹으로 이동하거나 삭제해주세요.");
                }

                await _sensorGroupRepository.DeleteAsync(groupId);

                _logger.LogInformation($"센서 그룹 삭제 완료 - GroupId: {groupId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"센서 그룹 삭제 실패 - GroupId: {groupId}, Error: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<IEnumerable<SensorDto>> GetGroupSensorsAsync(int groupId)
        {
            try
            {
                _logger.LogInformation($"그룹 센서 목록 조회 시작 - GroupId: {groupId}");

                var groupExists = await _sensorGroupRepository.ExistsAsync(groupId);
                if (!groupExists)
                {
                    throw new KeyNotFoundException($"센서 그룹을 찾을 수 없습니다. ID: {groupId}");
                }

                // Dapper Repository 메서드 사용
                var sensors = await _sensorRepository.GetByGroupIdAsDtoAsync(groupId);

                _logger.LogInformation($"그룹 센서 목록 조회 완료 - GroupId: {groupId}, 센서 수: {sensors.Count()}");
                return sensors;
            }
            catch (Exception ex)
            {
                _logger.LogError($"그룹 센서 목록 조회 실패 - GroupId: {groupId}, Error: {ex.Message}", ex);
                throw;
            }
        }

        public async Task<bool> GroupExistsAsync(int groupId)
        {
            return await _sensorGroupRepository.ExistsAsync(groupId);
        }
    }
}