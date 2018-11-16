import { AlertifyService } from './../../_services/alertify.service';
import { PaginatedResult } from './../../_models/pagination';
import { AdminService } from './../../_services/admin.service';
import { Component, OnInit } from '@angular/core';
import { Photo } from 'src/app/_models/photo';
import { Pagination } from 'src/app/_models/pagination';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-photo-managment',
  templateUrl: './photo-managment.component.html',
  styleUrls: ['./photo-managment.component.css']
})
export class PhotoManagmentComponent implements OnInit {
  photos: Photo[];
  pagination: Pagination;

  constructor(private adminService: AdminService, private alertify: AlertifyService,
    private route: ActivatedRoute) { }

  ngOnInit() {
    this.route.data.subscribe(data => {
      this.photos = data['photos'].result;
      this.pagination = data['photos'].pagination;
    });
    this.getPhotosForModeration();
  }

  pageChanged(event: any): void {
    this.pagination.currentPage = event.page;
    this.getPhotosForModeration();
  }

  getPhotosForModeration() {
    this.adminService
      .getPhotosForModeration(this.pagination.currentPage, this.pagination.itemsPerPage)
      .subscribe((res: PaginatedResult<Photo[]>) => {
        this.photos = res.result;
        this.pagination = res.pagination;
    }, error => {
      this.alertify.error(error);
    });
  }

  approvePhoto(id: number) {
    this.adminService.approvePhoto(id).subscribe(() => {
      this.alertify.success('Successfuly approved photo');
      this.photos.splice(this.photos.findIndex(p => p.id === id), 1);
      if (this.photos.length === 0) {
        this.pagination.currentPage = 1;
        this.getPhotosForModeration();
      }
    }, error => {
      this.alertify.error(error);
    });
  }

  rejectPhoto(id: number) {
    this.adminService.rejectPhoto(id).subscribe(() => {
      this.alertify.success('Successfuly rejected photo');
      this.photos.splice(this.photos.findIndex(p => p.id === id), 1);
      if (this.photos.length === 0) {
        this.pagination.currentPage = 1;
        this.getPhotosForModeration();
      }
    }, error => {
      this.alertify.error(error);
    });
  }
}
