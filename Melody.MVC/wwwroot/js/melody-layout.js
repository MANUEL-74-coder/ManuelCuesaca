// Funciones de navegación
function toggleSidebar() {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.querySelector('.sidebar-overlay');

    sidebar.classList.toggle('show');
    overlay.classList.toggle('show');
}

function closeSidebar() {
    const sidebar = document.getElementById('sidebar');
    const overlay = document.querySelector('.sidebar-overlay');

    sidebar.classList.remove('show');
    overlay.classList.remove('show');
}

function toggleUserMenu() {
    const userMenu = document.getElementById('userMenu');
    userMenu.classList.toggle('show');
}

// Cerrar el menú de usuario al hacer clic fuera de él
document.addEventListener('click', function (event) {
    const userDropdown = document.querySelector('.user-dropdown');
    const userMenu = document.getElementById('userMenu');

    if (userDropdown && !userDropdown.contains(event.target)) {
        userMenu.classList.remove('show');
    }
});

// Cerrar la barra lateral al cambiar de tamaño la ventana
window.addEventListener('resize', function () {
    if (window.innerWidth > 1024) {
        closeSidebar();
    }
});

// Manejar navegación activa
document.addEventListener('DOMContentLoaded', function () {
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.nav-section a');

    navLinks.forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });
});