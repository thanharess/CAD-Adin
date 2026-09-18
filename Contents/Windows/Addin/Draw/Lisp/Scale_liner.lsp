;;;=============================================
;;; Lệnh: SCALELINE
;;; Tác dụng: Scale giá trị "Linetype Scale" (CELTSCALE)
;;; Phiên bản sửa: Mode 3 đặt tất cả về 1 (không nhân/chia); Hỗ trợ scale trong BLOCK nếu chọn INSERT; Hoạt động trên locked layers; Ưu tiên xử lý BLOCK trước
;;;=============================================

(vl-load-com)

(defun c:SCALELINE ( / ss mode factor i en obj old new doc count blkname blkdef subobj)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (prompt "\nLệnh SCALELINE - Scale Linetype Scale.")

  ;; Chọn các đối tượng có Linetype Scale hoặc INSERT (cho phép chọn trên locked layers)
  (prompt "\nChọn các LINE/POLYLINE/ARC/CIRCLE/ELLIPSE/SPLINE hoặc BLOCK cần scale (Enter để kết thúc): ")
  (setq ss (ssget "_:L" '((0 . "LINE,LWPOLYLINE,2DPOLYLINE,3DPOLYLINE,ARC,CIRCLE,ELLIPSE,SPLINE,INSERT"))))
  (if (not ss)
    (progn (prompt "\nKhông có đối tượng nào được chọn.") (exit))
  )

  ;; Chọn mode bằng getkword (hiển thị menu dưới prompt, có thể click chuột chọn 1/2/3)
  (initget 1 "1 2 3")
  (setq mode (getkword "\nChọn chế độ [1:Tăng / 2:Giảm / 3:1:1] <1>: "))
  (if (not mode) (setq mode "1"))

  ;; Xử lý nhập hệ số chỉ cho mode 1 và 2; mode 3 không cần
  (cond
    ((= mode "1") 
     (setq factor (getreal "\nNhập hệ số tăng (VD: 2 = nhân 2:1): "))
     (if (not factor) (setq factor 1.0))
    )
    ((= mode "2") 
     (setq factor (getreal "\nNhập hệ số giảm (VD: 5 = chia 1:5): "))
     (if factor (setq factor (/ 1.0 factor)) (setq factor 1.0))
    )
    ((= mode "3") (setq factor 1.0))  ; Không dùng factor, chỉ đánh dấu
  )

  (if (or (not factor) (/= mode "3"))
    (if (not factor)
      (progn (prompt "\nKhông có hệ số hợp lệ.") (exit))
    )
  )

  ;; Khởi tạo bộ đếm đối tượng được scale
  (setq count 0)

  ;; Lặp qua từng đối tượng trong ss
  (setq i (sslength ss))
  (while (> i 0)
    (setq i (1- i))
    (setq en (ssname ss i))
    (setq obj (vlax-ename->vla-object en))
    (cond
      ;; Ưu tiên xử lý BLOCK REFERENCE (INSERT) - scale cả reference và subobjects trong definition
      ((= (vla-get-ObjectName obj) "AcDbBlockReference")
       ;; Scale LinetypeScale của block reference nếu có
       (if (vlax-property-available-p obj 'LinetypeScale)
         (progn
           (cond
             ((= mode "3")
              (vla-put-LinetypeScale obj 1.0)
             )
             (T
              (setq old (vla-get-LinetypeScale obj))
              (setq new (* old factor))
              (vla-put-LinetypeScale obj new)
             )
           )
           (setq count (1+ count))
         )
       )
       ;; Scale entities trong block definition
       (setq blkname (vla-get-EffectiveName obj))
       (if blkname
         (progn
           (setq blkdef (vla-Item (vla-get-Blocks doc) blkname))
           (vlax-for subobj blkdef
             (if (vlax-property-available-p subobj 'LinetypeScale)
               (progn
                 (cond
                   ((= mode "3")
                    (vla-put-LinetypeScale subobj 1.0)
                   )
                   (T
                    (setq old (vla-get-LinetypeScale subobj))
                    (setq new (* old factor))
                    (vla-put-LinetypeScale subobj new)
                   )
                 )
                 (setq count (1+ count))
               )
             )
           )
         )
       )
      )
      ;; Trường hợp là đối tượng khác có LinetypeScale (LINE, POLYLINE, ARC, etc.)
      ((vlax-property-available-p obj 'LinetypeScale)
       (cond
         ((= mode "3")
          (vla-put-LinetypeScale obj 1.0)
         )
         (T
          (setq old (vla-get-LinetypeScale obj))
          (setq new (* old factor))
          (vla-put-LinetypeScale obj new)
         )
       )
       (setq count (1+ count))
      )
    )
  )

  ;; Regen để cập nhật hiển thị
  (command "_.REGEN")

  (cond
    ((= mode "3")
     (prompt (strcat "\nĐã đặt " (itoa count) " đối tượng về Linetype Scale 1.0"))
    )
    (T
     (prompt (strcat "\nĐã scale " (itoa count)
                     " đối tượng, hệ số nhân: " (rtos factor 2 4)))
    )
  )
  (princ)
)

(princ "\nGõ SCALELINE để chạy lệnh. (Sử dụng chuột click vào menu dưới prompt để chọn nhanh)")
(princ)