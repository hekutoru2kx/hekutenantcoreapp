import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// Generic, owner-agnostic client for the content/attachments layer. There is deliberately no
// generic write endpoint on the backend — every upload/delete route belongs to the owning
// feature (e.g. UserController's /user/person/profile-picture) and enforces that feature's own
// permissions, then delegates server-side to the shared ContentService. This client mirrors
// that: fileUrl() is the one truly generic piece (the shared download endpoint); upload()/
// remove() just take whichever feature-owned path the caller passes in.
@Service()
export class Content {
  private http = inject(HttpClient);

  fileUrl(contentId: number): string {
    return `${environment.apiUrl}/content/${contentId}/file`;
  }

  // The download endpoint requires a bearer token, which only HttpClient (via the auth
  // interceptor) attaches — a plain <img src="..."> request goes out with no Authorization
  // header and 401s, and so does opening that URL directly. Callers bind the resulting blob as
  // an object URL instead (see AvatarUpload) so <img> never talks to the API directly.
  fetchImage(contentId: number): Observable<Blob> {
    return this.http.get(this.fileUrl(contentId), { responseType: 'blob' });
  }

  upload(path: string, file: File): Observable<{ id: number }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.put<{ id: number }>(`${environment.apiUrl}${path}`, formData);
  }

  remove(path: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}${path}`);
  }
}
