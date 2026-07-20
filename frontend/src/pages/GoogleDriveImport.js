import { useState, useEffect } from 'react';
import { getGoogleDriveStructure, importFromGoogleDrive, importFolderFromGoogleDrive } from '../services/api';
import { Link, useNavigate } from 'react-router-dom';
import { useToast } from '../components/Toast';

function formatBytes(bytes) {
  if (!bytes) return 'Unknown';
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  return (bytes / Math.pow(1024, i)).toFixed(1) + ' ' + sizes[i];
}

function GoogleDriveImport() {
  const [structure, setStructure] = useState({ folders: [], looseFiles: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [importing, setImporting] = useState(null);
  const [expandedFolders, setExpandedFolders] = useState(new Set());
  const navigate = useNavigate();
  const toast = useToast();

  useEffect(() => {
    loadStructure();
  }, []);

  const loadStructure = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await getGoogleDriveStructure();
      setStructure(res.data);
    } catch (err) {
      setError(err.response?.data?.error || 'Failed to load Google Drive structure');
    } finally {
      setLoading(false);
    }
  };

  const toggleFolder = (folderId) => {
    setExpandedFolders(prev => {
      const next = new Set(prev);
      if (next.has(folderId)) next.delete(folderId);
      else next.add(folderId);
      return next;
    });
  };

  const handleImportFolder = async (folder, e) => {
    e.stopPropagation();
    if (!window.confirm(`Import folder "${folder.name}" as a series? This will create ${folder.files.length} episode(s).`)) return;

    setImporting(folder.id);
    try {
      const res = await importFolderFromGoogleDrive({ folderId: folder.id });
      toast.success(`Series "${res.data.series.title}" created with ${res.data.episodes.length} episode(s)${res.data.skippedCount > 0 ? ` (${res.data.skippedCount} skipped)` : ''}.`);
      navigate('/');
    } catch (err) {
      toast.error('Failed: ' + (err.response?.data?.error || err.message));
    } finally {
      setImporting(null);
    }
  };

  const handleImportFile = async (file, e) => {
    if (e) e.stopPropagation();
    setImporting(file.id);
    try {
      const nameWithoutExt = file.name.replace(/\.[^/.]+$/, '');
      await importFromGoogleDrive({
        fileId: file.id,
        title: nameWithoutExt,
        releaseYear: new Date().getFullYear(),
      });
      toast.success(`"${file.name}" imported!`);
      navigate('/');
    } catch (err) {
      toast.error('Failed: ' + (err.response?.data?.error || err.message));
    } finally {
      setImporting(null);
    }
  };

  return (
    <div className="max-w-5xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-4">
          <Link to="/" className="bg-gray-700 hover:bg-gray-600 px-4 py-2 rounded-lg text-sm transition">← Back</Link>
          <h1 className="text-3xl font-bold">Import from Google Drive</h1>
        </div>
        <button onClick={loadStructure} className="bg-gray-700 hover:bg-gray-600 px-4 py-2 rounded-lg text-sm transition">
          Refresh
        </button>
      </div>

      {error && (
        <div className="bg-red-900/50 border border-red-700 text-red-300 px-4 py-3 rounded-lg mb-6">
          {error}
          <p className="text-sm mt-1">Make sure Google Drive credentials are configured.</p>
        </div>
      )}

      {loading ? (
        <div className="text-center py-20 text-gray-500">Loading structure from Google Drive...</div>
      ) : (
        <>
          {/* Folders */}
          {structure.folders.length > 0 && (
            <div className="mb-8">
              <h2 className="text-xl font-semibold mb-3 text-blue-400">📁 Folders ({structure.folders.length})</h2>
              <div className="space-y-3">
                {structure.folders.map(folder => {
                  const isExpanded = expandedFolders.has(folder.id);
                  return (
                    <div key={folder.id} className="bg-gray-800 rounded-xl overflow-hidden">
                      <div
                        onClick={() => toggleFolder(folder.id)}
                        className="flex items-center gap-4 p-4 bg-gray-750 cursor-pointer hover:bg-gray-700 transition"
                      >
                        <div className="text-2xl flex-shrink-0 transition-transform" style={{ transform: isExpanded ? 'rotate(90deg)' : 'rotate(0deg)' }}>
                          ▶
                        </div>
                        <div className="w-12 h-12 bg-blue-900/50 rounded-lg flex items-center justify-center text-xl flex-shrink-0">
                          📁
                        </div>
                        <div className="flex-1 min-w-0">
                          <h3 className="font-semibold truncate">{folder.name}</h3>
                          <p className="text-sm text-gray-400">{folder.files.length} file(s) — click to {isExpanded ? 'collapse' : 'expand'}</p>
                        </div>
                        <button
                          onClick={(e) => handleImportFolder(folder, e)}
                          disabled={importing === folder.id}
                          className="bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 px-4 py-2 rounded-lg text-sm font-medium transition flex-shrink-0"
                        >
                          {importing === folder.id ? 'Importing...' : 'Import as Series'}
                        </button>
                      </div>

                      {isExpanded && (
                        <div className="px-4 pb-4 pt-2 border-t border-gray-700 bg-gray-850">
                          <p className="text-xs text-gray-500 uppercase tracking-wide mb-2 px-2">Files in this folder — import individually as movie:</p>
                          <div className="space-y-2">
                            {folder.files.map(f => (
                              <div key={f.id} className="flex items-center gap-3 bg-gray-900 rounded-lg p-3">
                                <div className="w-10 h-10 bg-gray-700 rounded flex items-center justify-center text-lg flex-shrink-0">
                                  🎬
                                </div>
                                <div className="flex-1 min-w-0">
                                  <h4 className="text-sm font-medium truncate">{f.name}</h4>
                                  <p className="text-xs text-gray-500">{formatBytes(f.size)}</p>
                                </div>
                                <button
                                  onClick={(e) => handleImportFile(f, e)}
                                  disabled={importing === f.id}
                                  className="bg-red-600 hover:bg-red-700 disabled:bg-gray-600 px-3 py-1.5 rounded text-xs font-medium transition flex-shrink-0"
                                >
                                  {importing === f.id ? 'Importing...' : 'Import as Movie'}
                                </button>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Loose files (in Drive root) */}
          {structure.looseFiles.length > 0 && (
            <div className="mb-8">
              <h2 className="text-xl font-semibold mb-3 text-gray-400">📄 Loose Files ({structure.looseFiles.length})</h2>
              <p className="text-sm text-gray-500 mb-3">Files in Drive root — will be imported as standalone movies.</p>
              <div className="space-y-3">
                {structure.looseFiles.map(file => (
                  <div key={file.id} className="bg-gray-800 rounded-xl p-4 flex items-center gap-4">
                    <div className="w-12 h-12 bg-gray-700 rounded-lg flex items-center justify-center text-xl flex-shrink-0">
                      🎬
                    </div>
                    <div className="flex-1 min-w-0">
                      <h3 className="font-semibold truncate">{file.name}</h3>
                      <p className="text-sm text-gray-400">{formatBytes(file.size)} • {file.mimeType}</p>
                    </div>
                    <button
                      onClick={(e) => handleImportFile(file, e)}
                      disabled={importing === file.id}
                      className="bg-red-600 hover:bg-red-700 disabled:bg-gray-600 px-4 py-2 rounded-lg text-sm font-medium transition flex-shrink-0"
                    >
                      {importing === file.id ? 'Importing...' : 'Import'}
                    </button>
                  </div>
                ))}
              </div>
            </div>
          )}

          {structure.folders.length === 0 && structure.looseFiles.length === 0 && (
            <div className="text-center py-20 text-gray-500">
              No files or folders found in Google Drive.
            </div>
          )}
        </>
      )}
    </div>
  );
}

export default GoogleDriveImport;