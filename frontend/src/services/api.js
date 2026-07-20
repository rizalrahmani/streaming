import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
});

export const getMovies = () => api.get('/movies');
export const getMovie = (id) => api.get(`/movies/${id}`);
export const createMovie = (movie) => api.post('/movies', movie);
export const updateMovie = (movie) => api.put(`/movies/${movie.id}`, movie);
export const deleteMovie = (id) => api.delete(`/movies/${id}`);
export const getEpisodes = (id) => api.get(`/movies/${id}/episodes`);
export const getStreamUrl = (id) => `/api/movies/${id}/stream`;

export const getGoogleDriveFiles = (mimeType) => api.get('/googledrive/files', { params: { mimeType } });
export const getGoogleDriveStructure = () => api.get('/googledrive/structure');
export const importFromGoogleDrive = (data) => api.post('/googledrive/import', data);
export const importFolderFromGoogleDrive = (data) => api.post('/googledrive/import-folder', data);