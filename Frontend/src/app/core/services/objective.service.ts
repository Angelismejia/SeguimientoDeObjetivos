import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Objective, CreateObjectiveDto, UpdateObjectiveDto, ObjectiveSocial, ObjectiveComment
} from '../models/objective.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ObjectiveService {
  private url = `${environment.apiUrl}/objectives`;

  constructor(private http: HttpClient) {}

  getAll(userId: number): Observable<Objective[]> {
    return this.http.get<Objective[]>(`${this.url}?userId=${userId}`);
  }

  getById(id: number): Observable<Objective> {
    return this.http.get<Objective>(`${this.url}/${id}`);
  }

  create(data: CreateObjectiveDto, userId: number): Observable<Objective> {
    return this.http.post<Objective>(`${this.url}?userId=${userId}`, data);
  }

  update(id: number, data: UpdateObjectiveDto): Observable<Objective> {
    return this.http.put<Objective>(`${this.url}/${id}`, data);
  }

  setPrimary(id: number, userId: number, isPrimary: boolean): Observable<Objective> {
    return this.http.put<Objective>(`${this.url}/${id}/primary?userId=${userId}`, { isPrimary });
  }

  setPrivate(id: number, userId: number, isPrivate: boolean): Observable<Objective> {
    return this.http.put<Objective>(`${this.url}/${id}/privacy?userId=${userId}`, { isPrivate });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }

  // ── Comunidad: likes y comentarios en objetivos logrados ──

  getSocial(userId: number): Observable<ObjectiveSocial[]> {
    return this.http.get<ObjectiveSocial[]>(`${this.url}/social?userId=${userId}`);
  }

  setLike(id: number, liked: boolean): Observable<ObjectiveSocial> {
    return liked
      ? this.http.put<ObjectiveSocial>(`${this.url}/${id}/like`, {})
      : this.http.delete<ObjectiveSocial>(`${this.url}/${id}/like`);
  }

  getComments(id: number): Observable<ObjectiveComment[]> {
    return this.http.get<ObjectiveComment[]>(`${this.url}/${id}/comments`);
  }

  addComment(id: number, content: string): Observable<ObjectiveComment> {
    return this.http.post<ObjectiveComment>(`${this.url}/${id}/comments`, { content });
  }

  deleteComment(commentId: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/comments/${commentId}`);
  }
}
