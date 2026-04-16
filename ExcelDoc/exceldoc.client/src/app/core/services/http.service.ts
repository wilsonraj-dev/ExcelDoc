import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class HttpService {
  constructor(private readonly httpClient: HttpClient) { }

  get<T>(url: string): Observable<T> {
    return this.httpClient.get<T>(url);
  }

  post<TResponse, TRequest>(url: string, body: TRequest): Observable<TResponse> {
    return this.httpClient.post<TResponse>(url, body);
  }

  put<TResponse, TRequest>(url: string, body: TRequest): Observable<TResponse> {
    return this.httpClient.put<TResponse>(url, body);
  }

  delete<T>(url: string): Observable<T> {
    return this.httpClient.delete<T>(url);
  }
}
