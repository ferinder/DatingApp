import { Injectable } from '@angular/core';
import { CanDeactivate } from '@angular/router';
import { MemberEditComponent } from '../members/member-edit/member-edit.component';
import { AlertifyService } from '../_services/alertify.service';

@Injectable()
export class PreventUnsavedChanges implements CanDeactivate<MemberEditComponent> {
    canDeactivate(component: MemberEditComponent) {
        if (component.editForm.dirty) {
        //    this.alertify.confirm('Are you sure?', () => true);
            return confirm('Are you sure you want to continue? Any unsaved changes will be lost');
        }
        return true;
    }

    constructor(private alertify: AlertifyService) {}
}
