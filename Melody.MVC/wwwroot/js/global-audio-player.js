// Variables globales para el reproductor fijo
let globalCurrentSongIndex = 0;
let globalIsPlaying = false;
let globalPlaylist = [];
let globalAudioElement = null;

// Inicializar el reproductor global cuando se carga la página
document.addEventListener('DOMContentLoaded', function () {
    globalAudioElement = document.getElementById('globalAudioElement');

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

    // RESTAURAR MÚSICA GUARDADA (MOVIDO AQUÍ)
    const savedSong = localStorage.getItem('melody_currentSong');
    if (savedSong) {
        const songData = JSON.parse(savedSong);

        // Restaurar playlist y datos
        globalPlaylist = songData.playlist || [];
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

    // Ocultar el reproductor local si existe
    const localPlayer = document.getElementById('audioPlayer');
    if (localPlayer) {
        localPlayer.style.display = 'none';
    }
});

// Función para inicializar el reproductor global desde cualquier página
function initGlobalPlayer(playlist) {
    globalPlaylist = playlist;
}

// Función para reproducir una canción desde cualquier página
function playGlobalSong(audioUrl, title, artist, cover, songId) {
    // Agregar la canción a la playlist global si no existe
    if (!globalPlaylist.find(song => song.id === songId)) {
        globalPlaylist.push({
            audioUrl: audioUrl,
            title: title,
            artist: artist,
            cover: cover || '/images/default-song.png',
            id: songId
        });
    }

    // Encontrar el índice de la canción en la playlist global
    const songIndex = globalPlaylist.findIndex(song => song.id === songId);
    if (songIndex !== -1) {
        globalCurrentSongIndex = songIndex;
    }

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

// Función para reproducir toda la playlist desde cualquier página
function playGlobalPlaylist(playlist) {
    if (playlist && playlist.length > 0) {
        globalPlaylist = playlist;
        globalCurrentSongIndex = 0;
        const firstSong = globalPlaylist[0];
        playGlobalSong(firstSong.audioUrl, firstSong.title, firstSong.artist, firstSong.cover, firstSong.id);
    }
}

// Alternar entre reproducir y pausar en el reproductor global
function globalTogglePlayPause() {
    if (globalAudioElement.src) {
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
        playGlobalSong(prevSong.audioUrl, prevSong.title, prevSong.artist, prevSong.cover, prevSong.id);
    }
}

// Siguiente canción en el reproductor global
function globalNextSong() {
    if (globalPlaylist.length > 0) {
        globalCurrentSongIndex = (globalCurrentSongIndex + 1) % globalPlaylist.length;
        const nextSong = globalPlaylist[globalCurrentSongIndex];
        playGlobalSong(nextSong.audioUrl, nextSong.title, nextSong.artist, nextSong.cover, nextSong.id);
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
        playPauseBtn.innerHTML = '<i class="bi bi-pause-fill"></i>';
    } else {
        playPauseBtn.innerHTML = '<i class="bi bi-play-fill"></i>';
    }
}

// Resaltar la canción actual en cualquier tabla de la página
function globalHighlightCurrentSong() {
    // Remover highlight anterior
    document.querySelectorAll('tbody tr').forEach(row => {
        row.classList.remove('table-warning');
    });

    // Agregar highlight a la canción actual si existe en la página
    if (globalPlaylist[globalCurrentSongIndex]) {
        const currentSong = globalPlaylist[globalCurrentSongIndex];
        // Buscar la fila que contiene esta canción
        const buttons = document.querySelectorAll('button[onclick*="playSong"]');
        buttons.forEach(button => {
            if (button.getAttribute('onclick').includes(currentSong.id.toString())) {
                const row = button.closest('tr');
                if (row) {
                    row.classList.add('table-warning');
                }
            }
        });
    }
}

// Actualizar la barra de progreso global
function globalUpdateProgress() {
    if (globalAudioElement.duration) {
        const progress = (globalAudioElement.currentTime / globalAudioElement.duration) * 100;
        document.getElementById('globalProgressBar').value = progress;

        // Actualizar tiempos
        document.getElementById('globalCurrentTime').textContent = formatTime(globalAudioElement.currentTime);
        document.getElementById('globalTotalTime').textContent = formatTime(globalAudioElement.duration);
    }
}

// Buscar a una posición específica en la canción global
function globalSeekAudio(value) {
    if (globalAudioElement.duration) {
        const seekTime = (value / 100) * globalAudioElement.duration;
        globalAudioElement.currentTime = seekTime;
    }
}

// Función para formatear tiempo (reutilizable)
function formatTime(seconds) {
    if (isNaN(seconds)) return '0:00';

    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = Math.floor(seconds % 60);
    return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
}

// Modificar las funciones originales para usar el reproductor global
function playSong(audioUrl, title, artist, cover, songId) {
    // Si ya hay una playlist activa, solo cambiar la canción
    if (globalPlaylist.length > 0) {
        const songIndex = globalPlaylist.findIndex(song => song.id === songId);

        if (songIndex !== -1) {
            // Solo cambiar el índice y reproducir
            globalCurrentSongIndex = songIndex;
            const song = globalPlaylist[songIndex];

            // Configurar reproductor sin modificar la playlist
            globalAudioElement.src = song.audioUrl;
            globalAudioElement.load();
            globalUpdatePlayerUI(song.title, song.artist, song.cover);

            globalAudioElement.play().then(() => {
                globalIsPlaying = true;
                globalUpdatePlayPauseButton();
                globalHighlightCurrentSong();
            });
        }
    } else {
        // Si no hay playlist, crear una nueva (fallback)
        playAllSongs();
    }
}

function playAllSongs() {
    // Cargar la playlist desde la página actual
    const currentPagePlaylist = [];
    const rows = document.querySelectorAll('tbody tr');

    rows.forEach((row, index) => {
        const cells = row.cells;
        const playButton = cells[cells.length - 1].querySelector('button');

        if (playButton) {
            const onclickAttr = playButton.getAttribute('onclick');
            const matches = onclickAttr.match(/playSong\('([^']+)',\s*'([^']+)',\s*'([^']+)',\s*'([^']+)',\s*(\d+)\)/);

            if (matches) {
                currentPagePlaylist.push({
                    audioUrl: matches[1],
                    title: matches[2],
                    artist: matches[3],
                    cover: matches[4],
                    id: parseInt(matches[5])
                });
            }
        }
    });

    if (currentPagePlaylist.length > 0) {
        playGlobalPlaylist(currentPagePlaylist);
    }
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