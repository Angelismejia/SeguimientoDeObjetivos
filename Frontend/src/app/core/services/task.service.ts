import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TaskItem, CreateTaskDto, UpdateTaskDto } from '../models/task.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private url = `${environment.apiUrl}/tasks`;

  constructor(private http: HttpClient) {}

  getAll(userId: number): Observable<TaskItem[]> {
    return this.http.get<TaskItem[]>(`${this.url}?userId=${userId}`);
  }

  getById(id: number): Observable<TaskItem> {
    return this.http.get<TaskItem>(`${this.url}/${id}`);
  }

  create(data: CreateTaskDto, userId: number): Observable<TaskItem> {
    return this.http.post<TaskItem>(`${this.url}?userId=${userId}`, data);
  }

  update(id: number, data: UpdateTaskDto): Observable<TaskItem> {
    return this.http.put<TaskItem>(`${this.url}/${id}`, data);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
