import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable()
export class LanguageInterceptor implements HttpInterceptor {

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {

    const lang = localStorage.getItem('user_lang') ?? 'pt';

    let acceptLang = 'pt-BR';

    if (lang === 'en') {
      acceptLang = 'en-US';
    }

    if (lang === 'es') {
      acceptLang = 'es-ES';
    }

    const cloned = req.clone({
      setHeaders: {
        'Accept-Language': acceptLang
      }
    });

    return next.handle(cloned);
  }
}
