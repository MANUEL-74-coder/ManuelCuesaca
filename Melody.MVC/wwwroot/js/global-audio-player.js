// Variables globales para el reproductor fijo
let globalCurrentSongIndex = 0;
let globalIsPlaying = false;
let globalPlaylist = [];
let globalAudioElement = null;

// Inicializar el reproductor global cuando se carga la página
document.addEventListener('DOMContentLoaded', function () {
    globalAudioElement = document.getElementById('globalAudioElement');

    if (!globalAudioElement) {
        console.error('No se encontró el elemento de audio global');
        return;
    }

    // Eventos del reproductor global
    globalAudioElement.addEventListener('loadedmetadata', function () {
        document.getElementById('globalTotalTime').textContent = formatTime(globalAudioElement.duration);
    });

    globalAudioElement.addEventListener('play', function () {
        globalIsPlaying = true;
        globalUpdatePlayPauseButton();
    });

    globalAudioElement.addEventListener('pause', function () {
        globalIsPlaying = false;
        globalUpdatePlayPauseButton();
    });

    globalAudioElement.addEventListener('ended', function () {
        globalNextSong();
    });

    // Pre-cargar playlist si estamos en una página con canciones
    loadPagePlaylist();

    // RESTAURAR MÚSICA GUARDADA
    const savedSong = localStorage.getItem('melody_currentSong');
    if (savedSong) {
        const songData = JSON.parse(savedSong);

        // Restaurar playlist y datos
        if (songData.playlist && songData.playlist.length > 0) {
            globalPlaylist = songData.playlist;
        }
        globalCurrentSongIndex = songData.currentIndex || 0;

        // Configurar reproductor
        globalAudioElement.src = songData.src;
        globalAudioElement.currentTime = songData.currentTime;

        // Actualizar UI
        globalUpdatePlayerUI(songData.title, songData.artist, songData.cover);

        // Continuar reproduciendo automáticamente
        globalAudioElement.play().then(() => {
            globalIsPlaying = true;
            globalUpdatePlayPauseButton();
        }).catch(error => {
            console.log('Auto-play bloqueado por el navegador');
            globalIsPlaying = false;
            globalUpdatePlayPauseButton();
        });
    }
});

// Función para reproducir una canción desde cualquier página
function playSong(audioUrl, title, artist, cover, songId) {
    // Verificar que el elemento de audio existe
    if (!globalAudioElement) {
        console.error('Reproductor no inicializado');
        return;
    }

    // Si la playlist está vacía, cargar todas las canciones de la página
    if (globalPlaylist.length === 0) {
        loadPagePlaylist();
    }

    // Buscar la canción en la playlist existente
    let songIndex = globalPlaylist.findIndex(song => song.id === songId);

    // Si no está en la playlist, agregarla
    if (songIndex === -1) {
        globalPlaylist.push({
            audioUrl: audioUrl,
            title: title,
            artist: artist,
            cover: cover || '/images/default-song.png',
            id: songId
        });
        songIndex = globalPlaylist.length - 1;
    }

    // Establecer el índice actual
    globalCurrentSongIndex = songIndex;

    // Configurar el reproductor global
    globalAudioElement.src = audioUrl;
    globalAudioElement.load();

    // Actualizar la interfaz global
    globalUpdatePlayerUI(title, artist, cover);

    // Reproducir la canción
    globalAudioElement.play().then(() => {
        globalIsPlaying = true;
        globalUpdatePlayPauseButton();
        globalHighlightCurrentSong();
    }).catch(error => {
        console.error('Error al reproducir:', error);
    });
}

// Nueva función para cargar toda la playlist de la página actual
function loadPagePlaylist() {
    const currentPagePlaylist = [];
    const seenSongIds = new Set(); // Para evitar duplicados

    // Buscar todos los botones de reproducir en la página
    const playButtons = document.querySelectorAll('button[onclick*="playSong"]');

    playButtons.forEach(button => {
        const onclickAttr = button.getAttribute('onclick');
        // Expresión regular más robusta para extraer los parámetros
        const matches = onclickAttr.match(/playSong\('([^']+)',\s*'([^']+)',\s*'([^']+)',\s*'([^']+)',\s*(\d+)\)/);

        if (matches) {
            const songId = parseInt(matches[5]);

            // Solo agregar si no hemos visto esta canción antes
            if (!seenSongIds.has(songId)) {
                seenSongIds.add(songId);
                currentPagePlaylist.push({
                    audioUrl: matches[1],
                    title: matches[2],
                    artist: matches[3],
                    cover: matches[4],
                    id: songId
                });
            }
        }
    });

    // Solo actualizar si encontramos canciones
    if (currentPagePlaylist.length > 0) {
        globalPlaylist = currentPagePlaylist;
        console.log('Playlist cargada con', globalPlaylist.length, 'canciones (sin duplicados)');

        // Debug: mostrar los IDs de las canciones
        console.log('IDs de canciones:', globalPlaylist.map(song => song.id));
    }
}

// Alternar entre reproducir y pausar en el reproductor global
function globalTogglePlayPause() {
    if (globalAudioElement && globalAudioElement.src) {
        if (globalIsPlaying) {
            globalAudioElement.pause();
            globalIsPlaying = false;
        } else {
            globalAudioElement.play().then(() => {
                globalIsPlaying = true;
            }).catch(error => {
                console.error('Error al reproducir:', error);
            });
        }
        globalUpdatePlayPauseButton();
    }
}

// Canción anterior en el reproductor global
function globalPreviousSong() {
    if (globalPlaylist.length > 0) {
        globalCurrentSongIndex = (globalCurrentSongIndex - 1 + globalPlaylist.length) % globalPlaylist.length;
        const prevSong = globalPlaylist[globalCurrentSongIndex];
        console.log('Retrocediendo a canción:', prevSong.title, 'Índice:', globalCurrentSongIndex);
        playSong(prevSong.audioUrl, prevSong.title, prevSong.artist, prevSong.cover, prevSong.id);
    }
}

// Siguiente canción en el reproductor global
function globalNextSong() {
    if (globalPlaylist.length > 0) {
        const oldIndex = globalCurrentSongIndex;
        globalCurrentSongIndex = (globalCurrentSongIndex + 1) % globalPlaylist.length;
        const nextSong = globalPlaylist[globalCurrentSongIndex];
        console.log('Avanzando de índice', oldIndex, 'a', globalCurrentSongIndex, '- Canción:', nextSong.title);
        playSong(nextSong.audioUrl, nextSong.title, nextSong.artist, nextSong.cover, nextSong.id);
    } else {
        console.log('No hay playlist para avanzar');
    }
}

// Actualizar la interfaz del reproductor global
function globalUpdatePlayerUI(title, artist, cover) {
    document.getElementById('globalCurrentTitle').textContent = title;
    document.getElementById('globalCurrentArtist').textContent = artist;
    document.getElementById('globalCurrentCover').src = cover || '/images/default-song.png';
    document.getElementById('globalCurrentCover').alt = title;
}

// Actualizar el botón de play/pause global
function globalUpdatePlayPauseButton() {
    const playPauseBtn = document.getElementById('globalPlayPauseBtn');
    if (globalIsPlaying) {
        playPauseBtn.innerHTML = '⏸️';
    } else {
        playPauseBtn.innerHTML = '▶️';
    }
}

// Resaltar la canción actual en cualquier lista de la página
function globalHighlightCurrentSong() {
    // Remover highlight anterior
    document.querySelectorAll('.song-item').forEach(item => {
        item.classList.remove('current-playing');
    });

    // Agregar highlight a la canción actual si existe en la página
    if (globalPlaylist[globalCurrentSongIndex]) {
        const currentSong = globalPlaylist[globalCurrentSongIndex];
        const buttons = document.querySelectorAll('button[onclick*="playSong"]');
        buttons.forEach(button => {
            if (button.getAttribute('onclick').includes(currentSong.id.toString())) {
                const songItem = button.closest('.song-item');
                if (songItem) {
                    songItem.classList.add('current-playing');
                }
            }
        });
    }
}

// Actualizar la barra de progreso global
function globalUpdateProgress() {
    if (globalAudioElement && globalAudioElement.duration) {
        const progress = (globalAudioElement.currentTime / globalAudioElement.duration) * 100;
        document.getElementById('globalProgressBar').value = progress;

        // Actualizar tiempos
        document.getElementById('globalCurrentTime').textContent = formatTime(globalAudioElement.currentTime);
        document.getElementById('globalTotalTime').textContent = formatTime(globalAudioElement.duration);
    }
}

// Buscar a una posición específica en la canción global
function globalSeekAudio(value) {
    if (globalAudioElement && globalAudioElement.duration) {
        const seekTime = (value / 100) * globalAudioElement.duration;
        globalAudioElement.currentTime = seekTime;
    }
}

// Función para formatear tiempo
function formatTime(seconds) {
    if (isNaN(seconds)) return '0:00';

    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = Math.floor(seconds % 60);
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
}

// Controles de teclado para el reproductor global
document.addEventListener('keydown', function (e) {
    if (e.target.tagName.toLowerCase() !== 'input') {
        switch (e.code) {
            case 'Space':
                e.preventDefault();
                globalTogglePlayPause();
                break;
            case 'ArrowRight':
                globalNextSong();
                break;
            case 'ArrowLeft':
                globalPreviousSong();
                break;
        }
    }
});

// Guardar estado antes de cambiar de página
window.addEventListener('beforeunload', function () {
    if (globalAudioElement && globalAudioElement.src && !globalAudioElement.paused) {
        localStorage.setItem('melody_currentSong', JSON.stringify({
            src: globalAudioElement.src,
            currentTime: globalAudioElement.currentTime,
            title: document.getElementById('globalCurrentTitle').textContent,
            artist: document.getElementById('globalCurrentArtist').textContent,
            cover: document.getElementById('globalCurrentCover').src,
            playlist: globalPlaylist,
            currentIndex: globalCurrentSongIndex
        }));
    }
});

// Limpiar al cerrar pestaña completamente
window.addEventListener('unload', function () {
    localStorage.removeItem('melody_currentSong');
});

// FUNCIONES PARA ME GUSTA (AJAX)
function toggleMeGusta(cancionId, buttonElement) {
    buttonElement.disabled = true;
    fetch('/Canciones/ToggleMeGustaAjax', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'cancionId=' + cancionId
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                const isCurrentlyFavorite = buttonElement.dataset.esFavorito === 'true';
                const newState = !isCurrentlyFavorite;

                // Verificar si estamos en la página de favoritos
                if (window.location.pathname.includes('/MeGusta')) {
                    // En favoritos: remover la fila cuando se quita de favoritos
                    if (!newState) {
                        const songItem = buttonElement.closest('.song-item');
                        if (songItem) {
                            songItem.remove();
                            updateSongNumbers();
                            // Actualizar playlist del reproductor si está activa
                            updatePlaylistAfterRemoval(cancionId);
                        }
                    }
                } else {
                    // En otras páginas: cambiar el ícono del corazón
                    buttonElement.innerHTML = newState ? '♥' : '♡';
                }

                buttonElement.dataset.esFavorito = newState.toString();
                showToast(data.message, 'success');
            } else {
                showToast(data.message, 'error');
            }
        })
        .catch(() => showToast('Error al procesar favorito', 'error'))
        .finally(() => buttonElement.disabled = false);
}

// Función para actualizar playlist después de quitar favorito
function updatePlaylistAfterRemoval(cancionId) {
    // Solo actualizar si hay una playlist activa
    if (globalPlaylist.length > 0) {
        const removedIndex = globalPlaylist.findIndex(song => song.id === cancionId);

        if (removedIndex !== -1) {
            // Quitar la canción de la playlist
            globalPlaylist.splice(removedIndex, 1);

            // Ajustar el índice actual si es necesario
            if (globalCurrentSongIndex > removedIndex) {
                globalCurrentSongIndex--;
            } else if (globalCurrentSongIndex === removedIndex) {
                // Si se eliminó la canción que se está reproduciendo
                if (globalPlaylist.length === 0) {
                    // No quedan canciones, pausar reproductor
                    globalAudioElement.pause();
                    globalIsPlaying = false;
                    globalUpdatePlayPauseButton();
                    globalUpdatePlayerUI('Selecciona una canción', 'para reproducir', '/images/default-song.png');
                } else {
                    // Reproducir la siguiente canción (o la primera si era la última)
                    if (globalCurrentSongIndex >= globalPlaylist.length) {
                        globalCurrentSongIndex = 0;
                    }
                    const nextSong = globalPlaylist[globalCurrentSongIndex];
                    playSong(nextSong.audioUrl, nextSong.title, nextSong.artist, nextSong.cover, nextSong.id);
                }
            }

            console.log('Playlist actualizada. Canciones restantes:', globalPlaylist.length);
        }
    }
}

// Función para reordenar números en favoritos
function updateSongNumbers() {
    const songItems = document.querySelectorAll('.song-item');
    songItems.forEach((item, index) => {
        const numberSpan = item.querySelector('.song-number');
        if (numberSpan) {
            numberSpan.textContent = index + 1;
        }
    });
}

function showToast(message, type) {
    const toast = document.createElement('div');
    toast.className = `alert alert-${type === 'success' ? 'success' : 'danger'} alert-dismissible fade show position-fixed`;
    toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    toast.innerHTML = `${message}<button type="button" class="btn-close" onclick="this.parentElement.remove()"></button>`;
    document.body.appendChild(toast);
    setTimeout(() => toast.parentElement && toast.remove(), 3000);
}