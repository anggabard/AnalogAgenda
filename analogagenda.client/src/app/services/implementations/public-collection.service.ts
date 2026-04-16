import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CollectionPublicCommentDto,
  PublicCollectionPageDto,
} from '../../DTOs';

@Injectable({
  providedIn: 'root',
})
export class PublicCollectionService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/public/collections`;

  private opts = { withCredentials: true };

  getPage(collectionId: string): Observable<PublicCollectionPageDto> {
    return this.http.get<PublicCollectionPageDto>(`${this.baseUrl}/${collectionId}`, this.opts);
  }

  verify(collectionId: string, password: string): Observable<{ ok: boolean }> {
    return this.http.post<{ ok: boolean }>(
      `${this.baseUrl}/${collectionId}/verify`,
      { password },
      this.opts
    );
  }

  postComment(
    collectionId: string,
    body: { authorName: string; body: string }
  ): Observable<CollectionPublicCommentDto> {
    return this.http.post<CollectionPublicCommentDto>(
      `${this.baseUrl}/${collectionId}/comments`,
      body,
      this.opts
    );
  }

  downloadAll(collectionId: string, small: boolean): Observable<Blob> {
    const q = small ? '?small=true' : '';
    return this.http.get(`${this.baseUrl}/${collectionId}/download${q}`, {
      ...this.opts,
      responseType: 'blob',
    });
  }

  downloadSelected(collectionId: string, ids: string[], small: boolean): Observable<Blob> {
    return this.http.post(
      `${this.baseUrl}/${collectionId}/download/selected`,
      { ids, small },
      { ...this.opts, responseType: 'blob' }
    );
  }

  /** Full-resolution download for one photo (requires access cookie). */
  downloadPhoto(collectionId: string, photoId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${collectionId}/photos/${photoId}/download`, {
      ...this.opts,
      responseType: 'blob',
    });
  }
}
