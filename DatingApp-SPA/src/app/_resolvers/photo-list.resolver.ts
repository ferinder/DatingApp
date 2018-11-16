import { AdminService } from './../_services/admin.service';
import { Photo } from './../_models/photo';
import { Injectable } from '@angular/core';
import { Resolve, Router, ActivatedRouteSnapshot } from '@angular/router';
import { AlertifyService } from '../_services/alertify.service';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

@Injectable()
export class PhotoListResolver implements Resolve<Photo[]> {
  pageNumber = 1;
  pageSize = 5;

    constructor(private adminService: AdminService, private router: Router,
        private alertify: AlertifyService) { }

    resolve(route: ActivatedRouteSnapshot): Observable<Photo[]> {
        return this.adminService.getPhotosForModeration(this.pageNumber, this.pageSize)
          .pipe(
            catchError(error => {
                this.alertify.error('Problem retriving data.');
                this.router.navigate(['/home']);
                return of(null);
            })
        );
    }
}
