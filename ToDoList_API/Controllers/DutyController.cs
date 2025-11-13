using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using ToDoList_Core.Domain.Implementation;
using ToDoList_Core.Services.Interfaces;
using ToDoListAPI.DTOs.Duty;
using ToDoListAPI.DTOs.User;

namespace ToDoListAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class DutyController : ControllerBase
    {
        private readonly IDutyService _dutyService;
        private readonly IMapper _mapper;
        public DutyController(IDutyService dutyService, IMapper mapper)
        {
            _dutyService = dutyService;
            _mapper = mapper;
        }
        [HttpPost]
        public async Task<IActionResult> HtttpDutyRegister([FromBody] RegisterDutyDTO dutyDTO)
        {
            try
            {
                Duty dutyMapped = _mapper.Map(dutyDTO, new Duty());
                await _dutyService.CreateDuty(dutyMapped);
                return Ok(dutyMapped);
            }
            catch (Exception ex)
            {
                return BadRequest($"Hubo un error en la creacion de la tarea: {ex}");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Duty>> HtttpSearchDuty([FromRoute] int id)
        {
            try
            {
                return await _dutyService.FindDuty(u => u.Id == id);
            }
            catch (Exception) 
            {
                return BadRequest("Error");
            }
        }
        [HttpGet("ByUser/{userId}")]
        public async Task<ActionResult<List<Duty>>> HttpGetAllUserDuties([FromRoute] int userId)
        {
            try
            {
                var duties = await _dutyService.GetDutiesForUserAsync(userId);
                return Ok(_mapper.Map<IEnumerable<RegisterDutyDTO>>(duties));
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex}");
            }
        }
        [HttpPut]
        public async Task<IActionResult> HttpUpdateDuty([FromQuery] int id, [FromBody] RegisterDutyDTO dutyDTO)
        {
            try
            {
                var dutyToUpdate = await _dutyService.FindDuty(u => u.Id == id);
                await _dutyService.UpdateDutyAsync(dutyToUpdate);
                return Ok(dutyToUpdate);

            }catch (Exception ex){
                return BadRequest($"Error: {ex}");
                }
        }
        [HttpDelete]
        public async Task<IActionResult> HttpDeleteDuty([FromQuery] int id)
        {
            try
            {
                var dutyToDelete = await _dutyService.FindDuty(u => u.Id == id);
                await _dutyService.DeleteDuty(dutyToDelete);
                return Ok(dutyToDelete);
            }catch(Exception ex){
               return BadRequest($"Error: {ex}");
            }
        }
    }
}
