import { Pipe, PipeTransform } from '@angular/core';
import { TranslateService } from '../../core/services/translate.service';

@Pipe({ name: 'translate', pure: false })
export class TranslatePipe implements PipeTransform {
  constructor(private translate: TranslateService) {}

  transform(key: string): string {
    return this.translate.instant(key);
  }
}
