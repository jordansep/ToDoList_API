// ==========================================================================
//  CONFIGURACIÓN & CONSTANTES
// ==========================================================================
const DEFAULT_LOCAL_API = 'https://localhost:7163';
const DEFAULT_PROD_API = 'https://todolist-api-production.onrender.com'; // Cambiar por la URL real en Render

// Obtener la URL base de la API automáticamente o desde localStorage
function getApiUrl() {
  const savedUrl = localStorage.getItem('todolist_api_url');
  if (savedUrl) return savedUrl;
  
  const isLocal = window.location.hostname === 'localhost' || 
                  window.location.hostname === '127.0.0.1' || 
                  window.location.hostname === '::1';
                  
  return isLocal ? DEFAULT_LOCAL_API : DEFAULT_PROD_API;
}

let API_URL = getApiUrl();
let currentUser = null;
let allTasks = []; // Cache local para búsquedas y filtrados en tiempo real

// ==========================================================================
//  INICIALIZACIÓN DEL SISTEMA
// ==========================================================================
document.addEventListener('DOMContentLoaded', () => {
  initEventListeners();
  checkAuthSession();
});

// ==========================================================================
//  MANEJO DE SESIÓN Y AUTH
// ==========================================================================
function checkAuthSession() {
  const token = localStorage.getItem('todolist_jwt');
  if (token && isValidToken(token)) {
    currentUser = parseTokenClaims(token);
    currentUser.token = token;
    setupUserInterface();
    loadUserTasks();
  } else {
    logout(false); // Limpiar sesión inválida o caducada sin alertar agresivamente
  }
}

function isValidToken(token) {
  if (!token || !token.startsWith('ey')) return false;
  const claims = parseTokenClaims(token);
  if (!claims) return false;
  
  // Verificar expiración (exp está en segundos, Date.now() en ms)
  if (claims.exp && claims.exp * 1000 < Date.now()) {
    showToast('Sesión Expirada', 'Por favor, inicia sesión de nuevo.', 'info');
    return false;
  }
  return true;
}

function parseTokenClaims(token) {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(window.atob(base64).split('').map(c => {
      return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
    
    const rawClaims = JSON.parse(jsonPayload);
    
    // Mapeo flexible para reclamos estándar de C# y formas abreviadas
    return {
      id: rawClaims['nameid'] || rawClaims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
      email: rawClaims['email'] || rawClaims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
      role: rawClaims['role'] || rawClaims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'] || 'NormalUser',
      exp: rawClaims.exp
    };
  } catch (e) {
    console.error('Error parseando token:', e);
    return null;
  }
}

function setupUserInterface() {
  if (!currentUser) return;
  
  // Mostrar controles de usuario y cambiar de vista
  document.getElementById('header-user-controls').classList.remove('hidden');
  document.getElementById('view-auth').classList.add('hidden');
  document.getElementById('view-dashboard').classList.remove('hidden');
  
  // Configurar datos visuales en el header
  const initials = currentUser.email ? currentUser.email.substring(0, 2).toUpperCase() : 'U';
  document.getElementById('user-avatar-initials').textContent = initials;
  document.getElementById('user-display-name').textContent = currentUser.email.split('@')[0];
  document.getElementById('user-display-role').textContent = translateRole(currentUser.role);
  
  // Mensaje de bienvenida en el dashboard
  document.getElementById('dashboard-welcome-msg').textContent = `¡Hola, ${currentUser.email.split('@')[0]}!`;
  
  // Rellenar correos en los campos de perfil
  document.getElementById('profile-new-email').value = currentUser.email;
  document.getElementById('profile-new-email-confirm').value = currentUser.email;
}

function translateRole(role) {
  switch(role) {
    case 'Admin': return 'Administrador';
    case 'Manager': return 'Gestor';
    default: return 'Usuario Normal';
  }
}

function logout(notify = true) {
  localStorage.removeItem('todolist_jwt');
  currentUser = null;
  allTasks = [];
  
  document.getElementById('header-user-controls').classList.add('hidden');
  document.getElementById('view-dashboard').classList.add('hidden');
  document.getElementById('view-auth').classList.remove('hidden');
  
  // Limpiar formularios
  document.getElementById('form-login').reset();
  document.getElementById('form-register').reset();
  
  if (notify) {
    showToast('Sesión Cerrada', 'Has cerrado tu sesión correctamente.', 'success');
  }
}

// ==========================================================================
//  INTERFAZ Y NAVEGACIÓN SPA
// ==========================================================================
function switchAuthTab(tab) {
  const tabLogin = document.getElementById('tab-login');
  const tabRegister = document.getElementById('tab-register');
  const formLogin = document.getElementById('form-login');
  const formRegister = document.getElementById('form-register');
  
  if (tab === 'login') {
    tabLogin.classList.add('active');
    tabRegister.classList.remove('active');
    formLogin.classList.add('active-form');
    formLogin.classList.remove('hidden-form');
    formRegister.classList.remove('active-form');
    formRegister.classList.add('hidden-form');
  } else {
    tabRegister.classList.add('active');
    tabLogin.classList.remove('active');
    formRegister.classList.add('active-form');
    formRegister.classList.remove('hidden-form');
    formLogin.classList.remove('active-form');
    formLogin.classList.add('hidden-form');
  }
}

function switchProfileTab(tab) {
  const tabEmail = document.getElementById('profile-tab-email');
  const tabPassword = document.getElementById('profile-tab-password');
  const tabServer = document.getElementById('profile-tab-server');
  const formEmail = document.getElementById('form-update-email');
  const formPassword = document.getElementById('form-update-password');
  const formServer = document.getElementById('form-update-server');
  
  // Reset active classes
  tabEmail.classList.remove('active');
  tabPassword.classList.remove('active');
  tabServer.classList.remove('active');
  
  // Hide all forms
  formEmail.classList.add('hidden-form');
  formEmail.classList.remove('active-form');
  formPassword.classList.add('hidden-form');
  formPassword.classList.remove('active-form');
  formServer.classList.add('hidden-form');
  formServer.classList.remove('active-form');
  
  if (tab === 'email') {
    tabEmail.classList.add('active');
    formEmail.classList.add('active-form');
    formEmail.classList.remove('hidden-form');
  } else if (tab === 'password') {
    tabPassword.classList.add('active');
    formPassword.classList.add('active-form');
    formPassword.classList.remove('hidden-form');
  } else if (tab === 'server') {
    tabServer.classList.add('active');
    formServer.classList.add('active-form');
    formServer.classList.remove('hidden-form');
    // Populate with current API URL
    document.getElementById('profile-api-url').value = API_URL;
  }
}

// ==========================================================================
//  EVENT LISTENERS & MODALES
// ==========================================================================
function initEventListeners() {
  // Trigger Dropdown de Usuario
  const trigger = document.getElementById('user-profile-trigger');
  const menu = document.getElementById('user-dropdown-menu');
  trigger.addEventListener('click', (e) => {
    e.stopPropagation();
    menu.classList.toggle('show');
    trigger.classList.toggle('active');
  });
  
  document.addEventListener('click', () => {
    menu.classList.remove('show');
    trigger.classList.remove('active');
  });

  // Login
  document.getElementById('form-login').addEventListener('submit', handleLogin);
  
  // Registro
  document.getElementById('form-register').addEventListener('submit', handleRegister);
  
  // Abrir Modal de Tarea
  document.getElementById('btn-add-task').addEventListener('click', () => openTaskModal());
  
  // Cerrar Modal de Tarea
  document.getElementById('btn-close-task-modal').addEventListener('click', closeTaskModal);
  document.getElementById('btn-cancel-task').addEventListener('click', closeTaskModal);
  document.getElementById('form-task').addEventListener('submit', handleSaveTask);

  // Abrir Modal de Perfil
  document.getElementById('btn-open-profile').addEventListener('click', () => {
    document.getElementById('modal-profile').classList.remove('hidden');
    switchProfileTab('email');
  });
  
  // Cerrar Modal de Perfil
  document.getElementById('btn-close-profile-modal').addEventListener('click', () => {
    document.getElementById('modal-profile').classList.add('hidden');
  });

  // Actualizar Email
  document.getElementById('form-update-email').addEventListener('submit', handleUpdateEmail);

  // Actualizar Contraseña
  document.getElementById('form-update-password').addEventListener('submit', handleUpdatePassword);

  // Actualizar Servidor API
  document.getElementById('form-update-server').addEventListener('submit', (e) => {
    e.preventDefault();
    const newUrl = document.getElementById('profile-api-url').value.trim();
    if (newUrl) {
      // Remover slash final para consistencia si el usuario lo pone
      const cleanUrl = newUrl.endsWith('/') ? newUrl.slice(0, -1) : newUrl;
      localStorage.setItem('todolist_api_url', cleanUrl);
      showToast('Servidor Guardado', 'Configuración guardada. Reiniciando la app...', 'success');
      setTimeout(() => {
        window.location.reload();
      }, 1200);
    }
  });

  // Cerrar sesión
  document.getElementById('btn-logout').addEventListener('click', () => logout(true));
  
  // Input de búsqueda
  document.getElementById('task-search-input').addEventListener('input', (e) => {
    filterAndRenderTasks(e.target.value);
  });
}

// Modales helpers
function openTaskModal(task = null) {
  const modal = document.getElementById('modal-task');
  const title = document.getElementById('modal-task-title');
  const form = document.getElementById('form-task');
  
  form.reset();
  document.getElementById('task-id').value = '';
  
  if (task) {
    title.textContent = 'Editar Tarea';
    document.getElementById('task-id').value = task.id;
    document.getElementById('task-headline').value = task.headLine;
    document.getElementById('task-description').value = task.description || '';
    
    if (task.startDate) {
      document.getElementById('task-start-date').value = formatDateTimeForInput(task.startDate);
    }
    if (task.finishDate) {
      document.getElementById('task-finish-date').value = formatDateTimeForInput(task.finishDate);
    }
  } else {
    title.textContent = 'Nueva Tarea';
    // Colocar fecha y hora actual por defecto en fecha de inicio
    const now = new Date();
    document.getElementById('task-start-date').value = formatDateTimeForInput(now);
  }
  
  modal.classList.remove('hidden');
}

function closeTaskModal() {
  document.getElementById('modal-task').classList.add('hidden');
}

function showLoader() {
  document.getElementById('global-loader').classList.remove('hidden');
}

function hideLoader() {
  document.getElementById('global-loader').classList.add('hidden');
}

// Formateo de fechas
function formatDateTimeForInput(dateTimeString) {
  if (!dateTimeString) return '';
  const date = new Date(dateTimeString);
  if (isNaN(date)) return '';
  const tzOffset = date.getTimezoneOffset() * 60000; // offset en ms
  const localISOTime = (new Date(date - tzOffset)).toISOString().slice(0, 16);
  return localISOTime;
}

function formatPrettyDate(dateString) {
  if (!dateString) return 'No definida';
  const date = new Date(dateString);
  if (isNaN(date)) return 'No definida';
  return date.toLocaleString('es-AR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}

// ==========================================================================
//  PETICIONES A LA API (LOGICA DE RED)
// ==========================================================================

// Login Handler
async function handleLogin(e) {
  e.preventDefault();
  const username = document.getElementById('login-username').value.trim();
  const password = document.getElementById('login-password').value;
  
  showLoader();
  try {
    const response = await fetch(`${API_URL}/api/Auth/Login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ Username: username, Password: password })
    });
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Error al iniciar sesión');
    }
    
    const token = await response.text();
    
    // Manejo de error especial en C# donde a veces devuelve el mensaje de fallo con código 200
    if (token === 'Contraseña incorrecta') {
      throw new Error('Contraseña incorrecta');
    }
    if (!token.startsWith('ey')) {
      throw new Error(token || 'Credenciales inválidas');
    }
    
    localStorage.setItem('todolist_jwt', token);
    checkAuthSession();
    showToast('¡Bienvenido!', 'Sesión iniciada correctamente.', 'success');
  } catch (err) {
    console.error(err);
    showToast('Fallo de Conexión', err.message || 'Usuario o contraseña incorrectos.', 'error');
  } finally {
    hideLoader();
  }
}

// Register Handler
async function handleRegister(e) {
  e.preventDefault();
  const name = document.getElementById('register-name').value.trim();
  const lastname = document.getElementById('register-lastname').value.trim();
  const username = document.getElementById('register-username').value.trim();
  const email = document.getElementById('register-email').value.trim();
  const password = document.getElementById('register-password').value;
  
  showLoader();
  try {
    const response = await fetch(`${API_URL}/api/Users/Register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        Name: name,
        LastName: lastname,
        Username: username,
        Email: email,
        Password: password
      })
    });
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Error en el registro');
    }
    
    showToast('Registro Exitoso', 'Tu cuenta ha sido creada. Ya puedes iniciar sesión.', 'success');
    switchAuthTab('login');
    document.getElementById('login-username').value = username;
  } catch (err) {
    console.error(err);
    showToast('Error de Registro', err.message || 'Hubo un error al crear la cuenta. Intente con otros datos.', 'error');
  } finally {
    hideLoader();
  }
}

// Cargar tareas del usuario
async function loadUserTasks() {
  if (!currentUser) return;
  try {
    const response = await fetch(`${API_URL}/api/Duty/ByUser`, {
      headers: {
        'Authorization': `Bearer ${currentUser.token}`
      }
    });
    
    if (!response.ok) {
      throw new Error('No se pudieron obtener tus tareas');
    }
    
    allTasks = await response.json();
    filterAndRenderTasks();
  } catch (err) {
    console.error(err);
    showToast('Error de Carga', 'No se pudieron recuperar las tareas del servidor.', 'error');
  }
}

// Crear o Actualizar Tarea
async function handleSaveTask(e) {
  e.preventDefault();
  
  const id = document.getElementById('task-id').value;
  const headline = document.getElementById('task-headline').value.trim();
  const description = document.getElementById('task-description').value.trim();
  const startDateVal = document.getElementById('task-start-date').value;
  const finishDateVal = document.getElementById('task-finish-date').value;
  
  const taskData = {
    headLine: headline,
    description: description,
    startDate: startDateVal ? new Date(startDateVal).toISOString() : null,
    finishDate: finishDateVal ? new Date(finishDateVal).toISOString() : null
  };
  
  showLoader();
  try {
    let response;
    
    if (id) {
      // Estamos editando. Necesitamos enviar el ID y también conservar el Status original.
      const originalTask = allTasks.find(t => t.id == id);
      taskData.id = parseInt(id);
      taskData.status = originalTask ? originalTask.status : 0;
      
      response = await fetch(`${API_URL}/api/Duty/Update/${id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${currentUser.token}`
        },
        body: JSON.stringify(taskData)
      });
    } else {
      // Nueva tarea (por defecto nace en ToDo = 0)
      taskData.status = 0;
      
      response = await fetch(`${API_URL}/api/Duty`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${currentUser.token}`
        },
        body: JSON.stringify(taskData)
      });
    }
    
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || 'Error al guardar la tarea');
    }
    
    showToast('Tarea Guardada', id ? 'Tarea modificada correctamente.' : 'Nueva tarea creada con éxito.', 'success');
    closeTaskModal();
    loadUserTasks();
  } catch (err) {
    console.error(err);
    showToast('Error de Operación', err.message || 'No se pudo guardar la tarea en la base de datos.', 'error');
  } finally {
    hideLoader();
  }
}

// Modificar estado de tarea (Mover de columna)
async function moveTaskStatus(taskId, newStatus) {
  const task = allTasks.find(t => t.id === taskId);
  if (!task) return;
  
  const updatedTask = {
    id: task.id,
    headLine: task.headLine,
    description: task.description,
    status: newStatus, // 0 = ToDo, 1 = InProcess, 2 = Finished
    startDate: task.startDate,
    finishDate: task.finishDate
  };
  
  showLoader();
  try {
    const response = await fetch(`${API_URL}/api/Duty/Update/${taskId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${currentUser.token}`
      },
      body: JSON.stringify(updatedTask)
    });
    
    if (!response.ok) {
      throw new Error('Error al cambiar de columna');
    }
    
    showToast('Estado Actualizado', 'El estado de la tarea ha cambiado.', 'info');
    loadUserTasks();
  } catch (err) {
    console.error(err);
    showToast('Error', 'No se pudo actualizar el estado de la tarea.', 'error');
  } finally {
    hideLoader();
  }
}

// Eliminar tarea
async function deleteTask(taskId) {
  if (!confirm('¿Estás seguro de que quieres eliminar esta tarea?')) return;
  
  showLoader();
  try {
    const response = await fetch(`${API_URL}/api/Duty/Delete/${taskId}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${currentUser.token}`
      }
    });
    
    if (!response.ok) {
      throw new Error('Error al eliminar la tarea');
    }
    
    showToast('Tarea Eliminada', 'La tarea se eliminó de tu lista.', 'success');
    loadUserTasks();
  } catch (err) {
    console.error(err);
    showToast('Error de Eliminación', 'No se pudo borrar la tarea.', 'error');
  } finally {
    hideLoader();
  }
}

// Actualizar Email
async function handleUpdateEmail(e) {
  e.preventDefault();
  
  const email = document.getElementById('profile-new-email').value.trim();
  const emailConfirm = document.getElementById('profile-new-email-confirm').value.trim();
  
  if (email !== emailConfirm) {
    showToast('Validación', 'Los correos electrónicos no coinciden.', 'error');
    return;
  }
  
  showLoader();
  try {
    const response = await fetch(`${API_URL}/api/Users/UpdateEmail/${currentUser.id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${currentUser.token}`
      },
      body: JSON.stringify({
        NewEmail: email,
        ConfirmNewEmail: emailConfirm
      })
    });
    
    if (!response.ok) {
      const text = await response.text();
      throw new Error(text || 'Error al actualizar email');
    }
    
    showToast('Email Actualizado', 'Tu correo ha sido modificado. Vuelve a iniciar sesión si se requiere.', 'success');
    document.getElementById('modal-profile').classList.add('hidden');
    
    // Actualizar datos en sesión local temporal
    currentUser.email = email;
    setupUserInterface();
  } catch (err) {
    console.error(err);
    showToast('Error', err.message || 'No se pudo cambiar el email.', 'error');
  } finally {
    hideLoader();
  }
}

// Actualizar Contraseña
async function handleUpdatePassword(e) {
  e.preventDefault();
  
  const currentPassword = document.getElementById('profile-current-password').value;
  const newPassword = document.getElementById('profile-new-password').value;
  const newPasswordConfirm = document.getElementById('profile-new-password-confirm').value;
  
  if (newPassword !== newPasswordConfirm) {
    showToast('Validación', 'Las nuevas contraseñas no coinciden.', 'error');
    return;
  }
  
  showLoader();
  try {
    const response = await fetch(`${API_URL}/api/Users/UpdatePassword/${currentUser.id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${currentUser.token}`
      },
      body: JSON.stringify({
        CurrentPassword: currentPassword,
        NewPassword: newPassword,
        ConfirmNewPassword: newPasswordConfirm
      })
    });
    
    if (!response.ok) {
      const text = await response.text();
      throw new Error(text || 'Error al cambiar contraseña');
    }
    
    showToast('Contraseña Cambiada', 'Tu contraseña ha sido actualizada.', 'success');
    document.getElementById('modal-profile').classList.add('hidden');
    document.getElementById('form-update-password').reset();
  } catch (err) {
    console.error(err);
    showToast('Error', err.message || 'Contraseña actual incorrecta o no válida.', 'error');
  } finally {
    hideLoader();
  }
}

// ==========================================================================
//  LÓGICA DE FILTRADO Y RENDERIZACIÓN
// ==========================================================================
function filterAndRenderTasks(query = '') {
  query = query.toLowerCase().trim();
  
  const filtered = query 
    ? allTasks.filter(t => 
        t.headLine.toLowerCase().includes(query) || 
        (t.description && t.description.toLowerCase().includes(query))
      )
    : allTasks;
    
  renderKanban(filtered);
}

function renderKanban(tasks) {
  const containerTodo = document.getElementById('tasks-todo');
  const containerProcess = document.getElementById('tasks-process');
  const containerFinished = document.getElementById('tasks-finished');
  
  // Limpiar contenedores
  containerTodo.innerHTML = '';
  containerProcess.innerHTML = '';
  containerFinished.innerHTML = '';
  
  let todoCount = 0;
  let processCount = 0;
  let doneCount = 0;
  
  tasks.forEach(task => {
    const card = createTaskCard(task);
    
    // Clasificar según estado (0 = ToDo, 1 = InProcess, 2 = Finished)
    switch(task.status) {
      case 0:
        containerTodo.appendChild(card);
        todoCount++;
        break;
      case 1:
        containerProcess.appendChild(card);
        processCount++;
        break;
      case 2:
        containerFinished.appendChild(card);
        doneCount++;
        break;
    }
  });
  
  // Actualizar contadores del DOM
  document.getElementById('count-todo').textContent = todoCount;
  document.getElementById('count-process').textContent = processCount;
  document.getElementById('count-finished').textContent = doneCount;
  
  document.getElementById('stat-todo-count').textContent = todoCount;
  document.getElementById('stat-process-count').textContent = processCount;
  document.getElementById('stat-done-count').textContent = doneCount;
}

function createTaskCard(task) {
  const card = document.createElement('div');
  card.className = `task-card task-${task.status === 0 ? 'todo' : task.status === 1 ? 'process' : 'finished'}`;
  card.setAttribute('data-id', task.id);
  
  // Escuchar click de edición en la tarjeta (excepto si hace click en los botones de acción)
  card.addEventListener('click', (e) => {
    if (!e.target.closest('button')) {
      openTaskModal(task);
    }
  });

  // Generar HTML interno
  const descText = task.description ? task.description : '<i>Sin descripción.</i>';
  const startText = formatPrettyDate(task.startDate);
  const finishText = formatPrettyDate(task.finishDate);
  
  // Determinar botones de movimiento de estado
  let moveButtonsHtml = '';
  if (task.status === 0) {
    moveButtonsHtml = `
      <button class="btn-move btn-move-forward" onclick="moveTaskStatus(${task.id}, 1)">
        <span>Iniciar</span> <i class="fa-solid fa-arrow-right"></i>
      </button>
    `;
  } else if (task.status === 1) {
    moveButtonsHtml = `
      <button class="btn-move btn-move-backward" onclick="moveTaskStatus(${task.id}, 0)">
        <i class="fa-solid fa-arrow-left"></i> <span>Pausar</span>
      </button>
      <button class="btn-move btn-move-forward" onclick="moveTaskStatus(${task.id}, 2)">
        <span>Terminar</span> <i class="fa-solid fa-circle-check"></i>
      </button>
    `;
  } else if (task.status === 2) {
    moveButtonsHtml = `
      <button class="btn-move btn-move-backward" onclick="moveTaskStatus(${task.id}, 1)">
        <i class="fa-solid fa-arrow-left"></i> <span>Reabrir</span>
      </button>
    `;
  }

  card.innerHTML = `
    <div class="task-card-header">
      <h4 class="task-card-title">${escapeHTML(task.headLine)}</h4>
    </div>
    <div class="task-card-desc">${descText}</div>
    <div class="task-card-dates">
      <div class="date-row">
        <i class="fa-solid fa-calendar-plus color-todo"></i>
        <span>Inicio: ${startText}</span>
      </div>
      <div class="date-row">
        <i class="fa-solid fa-calendar-check color-done"></i>
        <span>Límite: ${finishText}</span>
      </div>
    </div>
    <div class="task-card-actions">
      <div class="card-action-group">
        <button class="btn-icon btn-icon-primary" title="Editar" onclick="event.stopPropagation(); openTaskModalForId(${task.id})">
          <i class="fa-solid fa-pen-to-square"></i>
        </button>
        <button class="btn-icon btn-icon-danger" title="Eliminar" onclick="event.stopPropagation(); deleteTask(${task.id})">
          <i class="fa-solid fa-trash-can"></i>
        </button>
      </div>
      ${moveButtonsHtml}
    </div>
  `;
  
  return card;
}

// Helper para abrir modal por ID (usado en onclick inline del botón de edición)
window.openTaskModalForId = function(id) {
  const task = allTasks.find(t => t.id === id);
  if (task) openTaskModal(task);
};

// Exponer funciones necesarias de forma global para los onclicks en el DOM dinámico
window.moveTaskStatus = moveTaskStatus;
window.deleteTask = deleteTask;

function escapeHTML(str) {
  return str.replace(/[&<>'"]/g, 
    tag => ({
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      "'": '&#39;',
      '"': '&quot;'
    }[tag] || tag)
  );
}

// ==========================================================================
//  TOASTS DE NOTIFICACIÓN
// ==========================================================================
function showToast(title, message, type = 'info') {
  const container = document.getElementById('toast-container');
  const toast = document.createElement('div');
  toast.className = `toast toast-${type}`;
  
  let iconHtml = '';
  switch(type) {
    case 'success':
      iconHtml = '<i class="fa-solid fa-circle-check toast-icon"></i>';
      break;
    case 'error':
      iconHtml = '<i class="fa-solid fa-circle-xmark toast-icon"></i>';
      break;
    default:
      iconHtml = '<i class="fa-solid fa-circle-info toast-icon"></i>';
      break;
  }
  
  toast.innerHTML = `
    ${iconHtml}
    <div class="toast-content">
      <div class="toast-title">${title}</div>
      <div class="toast-message">${message}</div>
    </div>
  `;
  
  container.appendChild(toast);
  
  // Forzar reflow para animación
  setTimeout(() => toast.classList.add('show'), 50);
  
  // Destruir toast luego de 4 segundos
  setTimeout(() => {
    toast.classList.remove('show');
    setTimeout(() => toast.remove(), 300);
  }, 4000);
}
