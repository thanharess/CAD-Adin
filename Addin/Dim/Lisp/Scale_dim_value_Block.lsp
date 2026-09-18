;;;=============================================
;;; Lệnh: SCALEDIMVALUEBLOCK
;;; Tác dụng: Scale giá trị "Dim scale linear" (DIMLFAC)
;;; Phiên bản sửa: Mode 3 đặt tất cả về 1 (không nhân/chia); Hỗ trợ scale DIM trong BLOCK nếu chọn INSERT; Hoạt động trên locked layers; Tối ưu update block bằng cách chỉ update khi cần
;;;=============================================

(vl-load-com)

(defun c:SCALEDIMVALUEBLOCK ( / ss mode factor i en obj old new doc count blkname blkdef subobj updated-blks)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (prompt "\nLệnh SCALEDIMVALUEBLOCK - Scale Dim Linear factor.")

  ;; Chọn các DIM hoặc INSERT (cho phép chọn trên locked layers)
  (prompt "\nChọn các DIMENSION hoặc BLOCK cần scale (Enter để kết thúc): ")
  (setq ss (ssget "_:L" '((0 . "DIMENSION,INSERT"))))
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

  ;; Khởi tạo bộ đếm DIM được scale và list block cần update (tránh duplicate)
  (setq count 0)
  (setq updated-blks '())  ; List các block name đã update

  ;; Lặp qua từng đối tượng trong ss
  (setq i (sslength ss))
  (while (> i 0)
    (setq i (1- i))
    (setq en (ssname ss i))
    (setq obj (vlax-ename->vla-object en))
    (cond
      ;; Trường hợp là DIMENSION
      ((wcmatch (vla-get-ObjectName obj) "*Dimension*")
       (cond
         ((= mode "3")
          (vla-put-LinearScaleFactor obj 1.0)
         )
         (T
          (setq old (vla-get-LinearScaleFactor obj))
          (setq new (* old factor))
          (vla-put-LinearScaleFactor obj new)
         )
       )
       (setq count (1+ count))
      )
      ;; Trường hợp là BLOCK REFERENCE (INSERT) - scale DIM trong block definition
      ((= (vla-get-ObjectName obj) "AcDbBlockReference")
       (setq blkname (vla-get-EffectiveName obj))
       (if (and blkname (not (member blkname updated-blks)))  ; Tránh update duplicate block
         (progn
           (setq updated-blks (cons blkname updated-blks))
           (setq blkdef (vla-Item (vla-get-Blocks doc) blkname))
           (vlax-for subobj blkdef
             (if (wcmatch (vla-get-ObjectName subobj) "*Dimension*")
               (progn
                 (cond
                   ((= mode "3")
                    (vla-put-LinearScaleFactor subobj 1.0)
                   )
                   (T
                    (setq old (vla-get-LinearScaleFactor subobj))
                    (setq new (* old factor))
                    (vla-put-LinearScaleFactor subobj new)
                   )
                 )
                 (setq count (1+ count))
               )
             )
           )
           ;; Update block reference để hiển thị thay đổi ngay (chỉ update nếu có thay đổi)
           (vla-Update obj)
         )
       )
      )
    )
  )

  ;; RegenALL để cập nhật hiển thị toàn bộ (chỉ nếu có thay đổi block)
  (if updated-blks (command "_.REGENALL"))

  (cond
    ((= mode "3")
     (prompt (strcat "\nĐã đặt " (itoa count) " DIM về giá trị 1.0"))
    )
    (T
     (prompt (strcat "\nĐã scale " (itoa count)
                     " DIM, hệ số nhân: " (rtos factor 2 4)))
    )
  )
  (princ)
)

(princ "\nGõ SCALEDIMVALUEBLOCK để chạy lệnh. (Sử dụng chuột click vào menu dưới prompt để chọn nhanh)")
(princ)