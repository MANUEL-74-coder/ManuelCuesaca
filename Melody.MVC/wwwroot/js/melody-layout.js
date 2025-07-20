// melody-layout.js - Versión corregida
document.addEventListener('DOMContentLoaded', function () {
    // Toggle Sidebar
    const toggleButton = document.getElementById('toggle-sidebar');
    const sidebar = document.querySelector('.melody-sidebar');

    if (toggleButton && sidebar) {
        toggleButton.addEventListener('click', function (e) {
            e.stopPropagation(); // Evita que se propague el evento
            sidebar.classList.toggle('active');
        });
    }

    // Cerrar sidebar al hacer clic fuera (pero no en los enlaces internos)
    document.addEventListener('click', function (e) {
        if (window.innerWidth <= 768 && sidebar) {
            // Solo cerrar si el clic no es en el sidebar ni en el botón toggle
            if (!sidebar.contains(e.target) && !toggleButton.contains(e.target)) {
                sidebar.classList.remove('active');
            }
        }
    });

    // Evitar que los enlaces del sidebar cierren el sidebar automáticamente
    if (sidebar) {
        const sidebarLinks = sidebar.querySelectorAll('.nav-link');
        sidebarLinks.forEach(link => {
            link.addEventListener('click', function (e) {
                // Solo cerramos el sidebar en móviles DESPUÉS de que el enlace se procese
                if (window.innerWidth <= 768) {
                    setTimeout(() => {
                        sidebar.classList.remove('active');
                    }, 100); // Pequeño delay para que la navegación se complete
                }
            });
        });
    }

    // Cerrar sidebar al redimensionar la ventana si se pasa a desktop
    window.addEventListener('resize', function () {
        if (window.innerWidth > 768 && sidebar) {
            sidebar.classList.remove('active');
        }
    });
});